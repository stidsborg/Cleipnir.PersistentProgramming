using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.StorageEngine.SqlServer;

namespace Playground.SqlExample
{
    public static class P
    {
        public static void Do()
        {
            const string databaseName = "CleipnirTest";
            DatabaseHelper.CreateDatabaseIfNotExist("localhost", databaseName, "sa", "Pa55word");
            var storage = new SqlServerStorageEngine("TEST", DatabaseHelper.ConnectionString("localhost", databaseName, "sa", "Pa55word"));

            storage.Initialize();
            storage.Clear();

            var p1 = new Person {Name = "Ole"};
            var p2 = new Person {Name = "Hans"};
            p1.Other = p2;

            var objectStore = ObjectStore.New(storage);
            objectStore.Entangle(p1);

            objectStore.Persist();

            objectStore = ObjectStore.Load(storage, false);
            p1 = objectStore.Resolve<Person>();
            p1.Other = null;
            objectStore.Persist();
            objectStore = ObjectStore.Load(storage, false);
        }

        public class Person : IPersistable
        {
            public string Name { get; set; }
            public Person Other { get; set; }

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(Name), Name);
                sd.Set(nameof(Other), Other);
            }

            private static Person Deserialize(IReadOnlyDictionary<string, object> sd)
                => new Person() {Name = sd.Get<string>(nameof(Name)), Other = sd.Get<Person>(nameof(Other))};
        }
    }
}
