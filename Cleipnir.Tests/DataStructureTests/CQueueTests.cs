using System.Linq;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.DataStructureTests
{
    [TestClass]
    public class CQueueTests
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
            var l = new CQueue<int>();
            l.Enqueue(1);

            l.Dequeue();

            AttachAndPersist(l);

            Load().Count.ShouldBe(0);
        }

        [TestMethod]
        public void InsertAndRemoveAgainAndAgain()
        {
            var l = new CQueue<int>();
            Attach(l);
            for (var i = 0; i < 50; i++)
            {
                l.Enqueue(i);
                Persist();
                l = Load();
                l.Dequeue().ShouldBe(i);
                Persist();
                l  = Load();
                l.Count.ShouldBe(0);
            }
        }

        [TestMethod]
        public void ElementsAddedToListAreAddedToStateMap()
        {
            var l = new CQueue<int>();
            l.Enqueue(0);
            l.Enqueue(1);

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
            var l = new CQueue<int>();
            l.Enqueue(0);
            l.Enqueue(1);

            AttachAndPersist(l);

            l.Dequeue();

            Persist();

            l = Load();

            l.Count.ShouldBe(1);
            l.Single().ShouldBe(1);
        }

        [TestMethod]
        public void ElementsAddedAndOneOtherElementRemovedToListAreAddedToStateMap()
        {
            var l = new CQueue<int>();
            l.Enqueue(0);
            l.Enqueue(1);

            AttachAndPersist(l);

            l.Count.ShouldBe(2);

            l.Dequeue();

            Persist();

            l = Load();

            l.Count.ShouldBe(1);
            l.Single().ShouldBe(1);
        }

        [TestMethod]
        public void ElementsAddedAndRemovedToListAreAddedToStateMap()
        {
            var l = new CQueue<int>();
            l.Enqueue(0);
            l.Enqueue(1);

            AttachAndPersist(l);

            l.Dequeue();
            l.Dequeue();

            Persist();

            l = Load();
            l.Count.ShouldBe(0);
        }

        [TestMethod]
        public void ElementsAddedAndRemovedAndSetToListAreAddedToStateMap()
        {
            var l = new CQueue<int>();
            l.Enqueue(0);
            l.Enqueue(1);

            AttachAndPersist(l);

            l.Dequeue();
            Persist();

            l = Load();
            l.Count.ShouldBe(1);
            l.Dequeue().ShouldBe(1);
        }

        private CQueue<int> Load()
        {
            ObjectStore = ObjectStore.Load(StableStorageEngine, false);
            return ObjectStore.Resolve<CQueue<int>>();
        }

        private void Attach<T>(T t) => ObjectStore.Entangle(t);

        private void AttachAndPersist<T>(T t)
        {
            ObjectStore.Entangle(t);
            ObjectStore.Persist();
        }

        private void Persist() => ObjectStore.Persist();
    }
}
