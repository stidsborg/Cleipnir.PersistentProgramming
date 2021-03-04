using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.SqlServerStorageEngine
{
    [TestClass]
    public class GarbageCollectionTests
    {
        [TestMethod]
        public void GarbageCollectableObjectIsRemovedFromTheDatabase()
        {
            var testHelper = new TestHelper();

            var child = new Person("Son");
            var parent = new Person("Dad") {Child = child};

            var os = testHelper.NewObjectStore();
            os.Entangle(parent);
            os.Persist();

            using var conn = testHelper.CreateConnection();
            conn.ExecuteScalar<int>("SELECT COUNT(*) FROM KeyValues WHERE [Value] = '\"Son\"'").ShouldBe(1);
            conn.ExecuteScalar<int>("SELECT COUNT(*) FROM KeyValues WHERE [Value] = '\"Dad\"'").ShouldBe(1);

            parent.Child = null;
            os.Persist();

            conn.ExecuteScalar<int>("SELECT COUNT(*) FROM KeyValues WHERE [Value] = '\"Son\"'").ShouldBe(0);
            conn.ExecuteScalar<int>("SELECT COUNT(*) FROM KeyValues WHERE [Value] = '\"Dad\"'").ShouldBe(1);
        }

        private class Person : IPersistable
        {
            public string Name { get; }
            public Person Child { get; set; }

            public Person(string name) => Name = name;

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(Name), Name);
                sd.Set(nameof(Child), Child);
            }

            private static Person Deserialize(IReadOnlyDictionary<string, object> sd)
                => new Person(sd.Get<string>(nameof(Name))) {Child = sd.Get<Person>(nameof(Child))};
        }
    }
}
