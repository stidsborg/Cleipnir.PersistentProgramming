using System.Linq;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.DataStructureTests
{
    [TestClass]
    public class CSetTests
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
        public void ElementsAddedToListAreAddedToStateMap()
        {
            var set = new CSet<int> {0, 1};

            AttachAndPersist(set);

            set = Load<CSet<int>>();
            
            set.Count.ShouldBe(2);
            var orderElements = set.OrderBy(_ => _).ToArray();
            orderElements[0].ShouldBe(0);
            orderElements[1].ShouldBe(1);
        }

        [TestMethod]
        public void ElementsAddedAndOneElementRemovedToListAreAddedToStateMap()
        {
            var set = new CSet<int> {0, 1};

            AttachAndPersist(set);

            set.Remove(0);

            Persist();

            set = Load<CSet<int>>();

            set.Count.ShouldBe(1);
            set.Single().ShouldBe(1);
        }

        [TestMethod]
        public void ElementsAddedAndOneOtherElementRemovedToListAreAddedToStateMap()
        {
            var set = new CSet<int> { 0, 1 };

            AttachAndPersist(set);

            set.Remove(1);

            Persist();

            set = Load<CSet<int>>();

            set.Count.ShouldBe(1);
            set.Single().ShouldBe(0);
        }

        [TestMethod]
        public void ElementsAddedAndRemovedToListAreAddedToStateMap()
        {
            var set = new CSet<int> {0, 1};

            AttachAndPersist(set);

            set.Remove(0);
            set.Remove(1);

            Persist();

            set = Load<CSet<int>>();
            set.Count.ShouldBe(0);
        }

        private T Load<T>() => ObjectStore.Load(StableStorageEngine, false).Resolve<T>();

        private void AttachAndPersist<T>(T t)
        {
            ObjectStore.Entangle(t);
            ObjectStore.Persist();
        }

        private void Persist() => ObjectStore.Persist();
    }
}
