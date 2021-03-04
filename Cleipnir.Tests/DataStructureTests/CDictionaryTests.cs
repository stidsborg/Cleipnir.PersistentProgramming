using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.DataStructureTests
{
    [TestClass]
    public class CDictionaryTests
    {
        [TestInitialize]
        public void Initialize()
        {
            StableStorageEngine = new InMemoryStorageEngine();
            ObjectStore = ObjectStore.New(StableStorageEngine);
        }

        private InMemoryStorageEngine StableStorageEngine { get; set; }
        private ObjectStore ObjectStore { get; set; }

        [TestMethod]
        public void InsertElementsPersistAndReceivedThemAgain()
        {
            var d = new CDictionary<int, string>
            {
                [5] = "Hello", [10] = "World"
            };


            AttachAndPersist(d);

            d = Load();
            d.Count.ShouldBe(2);
            d[5].ShouldBe("Hello");
            d[10].ShouldBe("World");

            d.Remove(5);
            Persist();

            d = Load();
            d.Count.ShouldBe(1);
            d[10].ShouldBe("World");

            d[10] = "WORLD";

            Persist();

            d = Load();

            d[10].ShouldBe("WORLD");

            d.Set(10, "worlds");
            d.Remove(10);

            Persist();

            d = Load();
            d.Count.ShouldBe(0);
        }

        private CDictionary<int, string> Load()
        {
            ObjectStore = ObjectStore.Load(StableStorageEngine, false);
            return ObjectStore.Resolve<CDictionary<int, string>>();
        } 

        private void AttachAndPersist<T>(T t)
        {
            ObjectStore.Entangle(t);
            ObjectStore.Persist();
        }

        private void Persist() => ObjectStore.Persist();
    }
}
