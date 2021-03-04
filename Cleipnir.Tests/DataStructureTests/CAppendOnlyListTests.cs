using System.Linq;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.DataStructureTests
{
    [TestClass]
    public class CAppendOnlyListTests
    {
        [TestMethod]
        public void SerializeAndDeserializeEmptyList()
        {
            var storage = new InMemoryStorageEngine();
            var os = ObjectStore.New(storage);

            var l = new CAppendOnlyList<int>();
            
            l.ToArray().Length.ShouldBe(0);

            os.Entangle(l);
            os.Persist();

            os = ObjectStore.Load(storage, true);
            l = os.Resolve<CAppendOnlyList<int>>();

            l.ToArray().Length.ShouldBe(0);
        }

        [TestMethod]
        public void AddBeforeAndAfterSerializationAndDeserialization()
        {
            var storage = new InMemoryStorageEngine();
            var os = ObjectStore.New(storage);

            var l = new CAppendOnlyList<int> {1};

            l.ToArray()[0].ShouldBe(1);

            os.Entangle(l);
            os.Persist();

            os = ObjectStore.Load(storage, true);
            l = os.Resolve<CAppendOnlyList<int>>();

            l.ToArray().Length.ShouldBe(1);
            l.ToArray()[0].ShouldBe(1);
            
            l.Add(2);

            l.ToArray().Length.ShouldBe(2);
            l.ToArray()[0].ShouldBe(1);
            l.ToArray()[1].ShouldBe(2);

            os.Persist();
            os = ObjectStore.Load(storage, true);
            l = os.Resolve<CAppendOnlyList<int>>();

            l.ToArray().Length.ShouldBe(2);
            l.ToArray()[0].ShouldBe(1);
            l.ToArray()[1].ShouldBe(2);
        }
    }
}
