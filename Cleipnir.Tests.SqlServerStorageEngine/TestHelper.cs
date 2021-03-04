using System.Data.SqlClient;
using Cleipnir.ObjectDB;
using Cleipnir.StorageEngine.SqlServer;

namespace Cleipnir.Tests.SqlServerStorageEngine
{
    internal class TestHelper
    {
        private static readonly string ConnectionString = DatabaseHelper.ConnectionString("localhost", "CleipnirTests", "sa", "Pa55word");
        public StorageEngine.SqlServer.SqlServerStorageEngine StorageEngineEngine { get; } = new StorageEngine.SqlServer.SqlServerStorageEngine("TEST", ConnectionString);

        public TestHelper()
        {
            CreateDatabaseIfNotExist();
            StorageEngineEngine.Initialize();
            StorageEngineEngine.Clear();
        }

        public ObjectStore NewObjectStore() => ObjectStore.New(StorageEngineEngine);
        public ObjectStore LoadExistingObjectStore() => ObjectStore.Load(StorageEngineEngine);
        public void CreateDatabaseIfNotExist() => DatabaseHelper.CreateDatabaseIfNotExist("localhost", "CleipnirTests", "sa", "Pa55word");

        public SqlConnection CreateConnection()
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();

            return connection;
        }
    }
}
