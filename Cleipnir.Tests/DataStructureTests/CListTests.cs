using System.Linq;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.DataStructureTests
{
    [TestClass]
    public class CListTests
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
        public void InsertAndRemoveBeforePersist()
        {
            var l = new CList<int> {1};

            l.Remove(0);

            AttachAndPersist(l);

            Load().Count.ShouldBe(0);
        }

        [TestMethod]
        public void InsertAndRemoveAgainAndAgain()
        {
            var l = new CList<int>();
            for (var i = 0; i < 50; i++)
                l.Add(i);

            

        }

        [TestMethod]
        public void ElementsAddedToListAreAddedToStateMap()
        {
            var l = new CList<int> {0, 1};

            AttachAndPersist(l);

            l = Load();
            
            l.Count.ShouldBe(2);
            var orderElements = l.OrderBy(_ => _).ToArray();
            orderElements[0].ShouldBe(0);
            orderElements[1].ShouldBe(1);
        }

        [TestMethod]
        public void ElementsAddedAndOneElementRemovedToListAreAddedToStateMap()
        {
            var l = new CList<int> {0, 1};

            AttachAndPersist(l);

            l.Remove(0);

            Persist();

            l = Load();

            l.Count.ShouldBe(1);
            l.Single().ShouldBe(1);
        }

        [TestMethod]
        public void ElementsAddedAndOneOtherElementRemovedToListAreAddedToStateMap()
        {
            var l = new CList<int> { 0, 1 };

            AttachAndPersist(l);

            l.Remove(1);

            Persist();

            l = Load();

            l.Count.ShouldBe(1);
            l.Single().ShouldBe(0);
        }

        [TestMethod]
        public void ElementsAddedAndRemovedToListAreAddedToStateMap()
        {
            var l = new CList<int> {0, 1};

            AttachAndPersist(l);

            l.Remove(0);
            l.Remove(0);

            Persist();

            l = Load();
            l.Count.ShouldBe(0);
        }

        [TestMethod]
        public void ElementsAddedAndRemovedAndSetToListAreAddedToStateMap()
        {
            var l = new CList<int> { 0, 1 };

            AttachAndPersist(l);

            l.Remove(0);
            l[0] = 100;
            Persist();

            l = Load();
            l.Count.ShouldBe(1);
            l[0].ShouldBe(100);
        }

        private CList<int> Load() => ObjectStore.Load(StableStorageEngine, false).Resolve<CList<int>>();

        private void AttachAndPersist<T>(T t)
        {
            ObjectStore.Entangle(t);
            ObjectStore.Persist();
        }

        private void Persist() => ObjectStore.Persist();
    }
}
