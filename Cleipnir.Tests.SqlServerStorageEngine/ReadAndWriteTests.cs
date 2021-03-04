using System.Collections.Generic;
using Cleipnir.ObjectDB;
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
    public class ReadAndWriteTests
    {
        [TestMethod]
        public void ReadWriteTest()
        {
            var testHelper = new TestHelper();

            var os = ObjectStore.New(testHelper.StorageEngineEngine);

            var p = new P();
            os.Attach(p);
            p.SetValue("HELLO WORLD");
            os.Persist();

            ObjectStore.Load(testHelper.StorageEngineEngine)
                .Resolve<P>()
                .GetValue()
                .ShouldBe("HELLO WORLD");
            
            using var conn = testHelper.CreateConnection(); 
            conn.QuerySingle<int>(@"
                    SELECT COUNT(*)
                    FROM [KeyValues]
                    WHERE [Value] = '""HELLO WORLD""'"
            ).ShouldBe(1);

            p.RemoveValue();
            os.Persist();
            
            ObjectStore.Load(testHelper.StorageEngineEngine)
                .Resolve<P>()
                .GetValue()
                .ShouldBeNull();

            conn.QuerySingle<int>(@"
                    SELECT COUNT(*)
                    FROM [KeyValues]
                    WHERE [Value] = '""HELLO WORLD""'"
            ).ShouldBe(0);
        }

        private class P : IPersistable
        {
            private string _value;

            public void SetValue(string value) => _value = value;
            public string GetValue() => _value;

            public void RemoveValue() => _value = null;

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                if (_value == null)
                    sd.Remove(nameof(_value));
                else
                    sd.Set(nameof(_value), _value);
            }

            private static P Deserialize(IReadOnlyDictionary<string, object> sd)
            {
                return sd.ContainsKey(nameof(_value))
                    ? new P() { _value = sd.Get<string>(nameof(_value)) }
                    : new P();
            }
        }
    }
}
