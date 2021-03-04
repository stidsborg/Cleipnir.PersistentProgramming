using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Cleipnir.Helpers;
using Dapper;
using Newtonsoft.Json;

namespace Cleipnir.StorageEngine.SqlServer
{
    public class SqlServerStorageEngine : IStorageEngine
    {
        private readonly string _instanceId;
        private readonly string _connectionString;

        private volatile bool _initialized;

        public SqlServerStorageEngine(string instanceId, string connectionString)
        {
            _instanceId = instanceId;
            _connectionString = connectionString;
        }

        private SqlConnection CreateConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();

            return connection;
        }

        public void Clear()
        {
            using var connection = CreateConnection();
            connection.Execute($@"
                DELETE FROM KeyValues WHERE InstanceId='{_instanceId}';
                DELETE FROM SerializerTypes WHERE InstanceId='{_instanceId}';"
            );
        }

        public void DropTables()
        {
            using var connection = CreateConnection();
            connection.Execute(@"
                DROP TABLE IF EXISTS KeyValues;
                DROP TABLE IF EXISTS SerializerTypes;"
            );
        }

        public bool Exist
        {
            get
            {
                using var connection = CreateConnection();
                var count = connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM KeyValues WHERE InstanceId='{_instanceId}'");
                return count > 0;
            }
        }

        public void Initialize()
        {
            _initialized = true;

            using var connection = CreateConnection();
          
            connection.Execute(@"
                IF OBJECT_ID('KeyValues', 'U') IS NULL 
                  CREATE TABLE [KeyValues](
	                [InstanceId] [nvarchar](50) NOT NULL,
	                [ObjectId] [bigint] NOT NULL,
	                [Key] [nvarchar](50) NOT NULL,
	                [Value] [nvarchar](max) NULL,
                    [ValueType] [nvarchar](50) NULL,
	                [Reference] [bigint] NULL,
                    CONSTRAINT [PK_KeyValues] PRIMARY KEY CLUSTERED ([InstanceId] ASC, [ObjectId] ASC, [Key] ASC)
                )");
            
            connection.Execute(@"
                IF OBJECT_ID('SerializerTypes', 'U') IS NULL 
                  CREATE TABLE [SerializerTypes](
	                [InstanceId] [nvarchar](50) NOT NULL,
	                [ObjectId] [bigint] NOT NULL,
	                [SerializerType] [nvarchar](max) NULL,
                    CONSTRAINT [PK_SerializerTypes] PRIMARY KEY CLUSTERED ([InstanceId] ASC, [ObjectId] ASC)
                )");
        }

        public void Persist(DetectedChanges detectedChanges)
        {
            if (_initialized) Initialize();

            using var connection = CreateConnection();
            using var transaction = connection.BeginTransaction();

            //CREATE TEMP TABLES
            connection.Execute(@"
                CREATE TABLE [#GarbageCollectables] ( [ObjectId] [bigint] NOT NULL )
                CREATE TABLE [#KeyValues](
	                [InstanceId] [nvarchar](50) NOT NULL,
	                [ObjectId] [bigint] NOT NULL,
	                [Key] [nvarchar](50) NOT NULL,
	                [Value] [nvarchar](max) NULL,
                    [ValueType] [nvarchar](50) NULL,
	                [Reference] [bigint] NULL
                )
                CREATE TABLE [#RemovedEntries](
	                [ObjectId] [bigint] NOT NULL,
	                [Key] [nvarchar](50) NOT NULL
                )",
                transaction: transaction);

            AddSerializerTypes(detectedChanges.NewSerializerTypes, connection, transaction);
            UpsertEntries(detectedChanges.NewEntries, connection, transaction);
            RemoveRemovedEntries(detectedChanges.RemovedEntries, connection, transaction);
            RemoveGarbageCollectables(detectedChanges.GarbageCollectableIds, connection, transaction);

            transaction.Commit();
        }

        private void RemoveGarbageCollectables(IEnumerable<long> ids, SqlConnection connection, SqlTransaction transaction)
        {
            if (!ids.Any()) return;

            var dataTable = new DataTable("#GarbageCollectables");
            dataTable.Columns.Add("ObjectId", typeof(long));

            foreach (var id in ids)
                dataTable.Rows.Add(id);

            using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.KeepIdentity, transaction) {DestinationTableName = "#GarbageCollectables"};
            bulkCopy.WriteToServer(dataTable);

            connection.Execute(@$"
                DELETE kv 
                FROM KeyValues AS kv
                INNER JOIN #GarbageCollectables AS gc ON kv.ObjectId = gc.ObjectId AND kv.InstanceId = '{_instanceId}';
                DELETE s 
                FROM SerializerTypes AS s
                INNER JOIN #GarbageCollectables AS gc ON s.ObjectId = gc.ObjectId AND s.InstanceId = '{_instanceId}';",
                transaction: transaction
            );
        }

        private void RemoveRemovedEntries(IEnumerable<ObjectIdAndKey> entries, SqlConnection connection, SqlTransaction transaction)
        {
            if (!entries.Any()) return;

            var dataTable = new DataTable("#RemovedEntries");
            dataTable.Columns.Add("ObjectId", typeof(long));
            dataTable.Columns.Add("Key", typeof(string));

            foreach (var entry in entries)
                dataTable.Rows.Add(entry.ObjectId, entry.Key);

            using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.KeepIdentity, transaction) { DestinationTableName = "#RemovedEntries" };
            bulkCopy.WriteToServer(dataTable);

            connection.Execute(@$"
                DELETE kv 
                FROM KeyValues AS kv
                INNER JOIN #RemovedEntries AS gc 
                ON kv.ObjectId = gc.ObjectId AND kv.[Key] = gc.[Key] AND kv.InstanceId = '{_instanceId}';", 
                transaction: transaction
            );
        }

        private void AddSerializerTypes(IEnumerable<ObjectIdAndType> serializerTypes, SqlConnection connection, SqlTransaction transaction)
        {
            var dataTable = new DataTable("#SerializerTypes");

            var instanceId = new DataColumn("InstanceId", typeof(string));
            var objectId = new DataColumn("ObjectId", typeof(long));
            var key = new DataColumn("SerializerType", typeof(string));

            var columns = new[] { instanceId, objectId, key };
            dataTable.Columns.AddRange(columns);

            foreach (var serializerType in serializerTypes)
            {
                dataTable.Rows.Add(
                    _instanceId,
                    serializerType.ObjectId,
                    serializerType.SerializerType.SimpleQualifiedName()
                );
            }
            
            using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.KeepIdentity, transaction) { DestinationTableName = "SerializerTypes" };
            bulkCopy.WriteToServer(dataTable);
        }
        
        private void UpsertEntries(IEnumerable<StorageEntry> entries, SqlConnection connection, SqlTransaction transaction)
        {
            var dataTable = new DataTable("#KeyValues");

            var instanceId = new DataColumn("InstanceId", typeof(string));
            var objectId = new DataColumn("ObjectId", typeof(long));
            var key = new DataColumn("Key", typeof(string));
            var value = new DataColumn("Value", typeof(string));
            var valueType = new DataColumn("ValueType", typeof(string));
            var reference = new DataColumn("Reference", typeof(long));

            var columns = new[] { instanceId, objectId, key, value, valueType, reference };
            dataTable.Columns.AddRange(columns);
            dataTable.PrimaryKey = new[] { instanceId, objectId, key };

            foreach (var logEntry in entries)
            {
                var valueTypeValue = logEntry.Value?.GetType().FullName;
                dataTable.Rows.Add(
                    _instanceId,
                    logEntry.ObjectId,
                    logEntry.Key,
                    logEntry.Value?.ToJson(),
                    valueTypeValue,
                    logEntry.Reference
                );
            }

            using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.KeepIdentity, transaction) { DestinationTableName = "#KeyValues" };
            bulkCopy.WriteToServer(dataTable);

            connection.Execute($@"
                MERGE KeyValues AS t USING #KeyValues AS s
                ON t.InstanceId = s.InstanceId AND t.ObjectId = s.ObjectId AND t.[Key] = s.[Key]
                WHEN MATCHED 
                    THEN UPDATE SET t.[Value] = s.[Value], t.[Reference] = s.[Reference], t.[ValueType] = s.[ValueType]
                WHEN NOT MATCHED
                    THEN INSERT (InstanceId, ObjectId, [Key], [Value], [Reference], [ValueType])
                    VALUES (s.InstanceId, s.ObjectId, s.[Key], s.[Value], s.[Reference], s.[ValueType]);",
                transaction: transaction
            );
        }

        public StoredState Load()
        {
            if (!_initialized) Initialize();

            using var connection = CreateConnection();

            var keyValueEntries = connection
                .Query<KeyValueEntry>($"SELECT * FROM KeyValues WHERE InstanceId='{_instanceId}'");

            var serializerTypeEntries = connection
                .Query<SerializerTypeEntry>($"SELECT ObjectId, SerializerType FROM SerializerTypes WHERE InstanceId='{_instanceId}'");

            var keyValues = keyValueEntries
                .Select(e =>
                    new StorageEntry(
                        objectId: e.ObjectId,
                        key: e.Key,
                        value: e.ValueType == null
                            ? null
                            : JsonConvert.DeserializeObject(e.Value, Type.GetType(e.ValueType)),
                        reference: e.Reference
                    )
                );

            var serializerTypes = serializerTypeEntries
                .Select(e => new {e.ObjectId, Type = Type.GetType(e.SerializerType)});

            var storedState = new StoredState(
                serializerTypes.ToDictionary(
                    e => e.ObjectId,
                    e => e.Type),
                keyValues
                    .GroupBy(kv => kv.ObjectId, kv => kv)
                    .ToDictionary(g => g.Key, g => g.AsEnumerable())
                );
            return storedState;
        }

        private class KeyValueEntry 
        {
            public long ObjectId { get; set; }
            public string Key { get; set; }
            public string Value { get; set; }
            public string ValueType { get; set; }
            public long? Reference { get; set; }
        }

        private class SerializerTypeEntry
        {
            public long ObjectId { get; set; }
            public string SerializerType { get; set; }
        }

        public void Dispose() { } //todo clear temp created tables
    }
}
