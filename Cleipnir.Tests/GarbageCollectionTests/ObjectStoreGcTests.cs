using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB;
using Cleipnir.StorageEngine.InMemory;
using Cleipnir.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.GarbageCollectionTests
{
    [TestClass]
    public class ObjectStoreGcTests
    {
        private InMemoryStorageEngine _storageEngine;

        [TestInitialize]
        public void Initialize()
        {
            _storageEngine = new InMemoryStorageEngine();
        }

        [TestMethod]
        public void AddAndRemoveReferenceFromOnePersonToAnotherAndCheckItIsGarbageCollected()
        {
            var objectStore = ObjectStore.New(_storageEngine);

            var p1 = new Person { Name = "Ole" };
            var p2 = new Person { Name = "Hans" };
            p1.Other = p2;

            objectStore.Entangle(p1);
            objectStore.Persist();

            var p2ObjectId = FindPersonObjectId("Hans");

            objectStore = ObjectStore.Load(_storageEngine, true);
            p1 = objectStore.Resolve<Person>();
            p1.Other = null;

            objectStore.Persist();

            GetGarbageCollectable().Contains(p2ObjectId).ShouldBeTrue();
        }

        [TestMethod]
        public void AddAndRemoveReferenceFromOnePersonToAnotherThatHasAnotherReferenceAndCheckItIsGarbageCollected()
        {
            var objectStore = ObjectStore.New(_storageEngine);

            var p3 = new Person { Name = "P3" };
            var p2 = new Person { Name = "P2", Other =  p3};
            var p1 = new Person { Name = "P1", Other =  p2};
          
            objectStore.Entangle(p1);
            objectStore.Persist();

            var p2ObjectId = FindPersonObjectId("P2");
            var p3ObjectId = FindPersonObjectId("P3");

            objectStore = ObjectStore.Load(_storageEngine, true);
            p1 = objectStore.Resolve<Person>();
            p1.Other = null;

            objectStore.Persist();

            GetGarbageCollectable().Contains(p2ObjectId).ShouldBeTrue();
            GetGarbageCollectable().Contains(p3ObjectId).ShouldBeTrue();
            GetGarbageCollectable().Count().ShouldBe(2);
        }

        [TestMethod]
        public void AddAndRemoveReferenceAndNewPersonAgainFromOnePersonToAnotherThatHasAnotherReferenceAndCheckItIsGarbageCollected()
        {
            var objectStore = ObjectStore.New(_storageEngine);

            var p3 = new Person { Name = "P3" };
            var p2 = new Person { Name = "P2", Other = p3 };
            var p1 = new Person { Name = "P1", Other = p2 };

            objectStore.Entangle(p1);
            objectStore.Persist();

            var p2ObjectId = FindPersonObjectId("P2");
            var p3ObjectId = FindPersonObjectId("P3");

            objectStore = ObjectStore.Load(_storageEngine, true);
            p1 = objectStore.Resolve<Person>();
            p1.Other = new Person { Name = "P4" };

            objectStore.Persist();

            GetGarbageCollectable().Contains(p2ObjectId).ShouldBeTrue();
            GetGarbageCollectable().Contains(p3ObjectId).ShouldBeTrue();
            GetGarbageCollectable().Count().ShouldBe(2);
        }

        private long FindPersonObjectId(string name)
            => _storageEngine
                .Entries
                .Single(e => !e.IsReference && e.Value != null && e.Value.Equals(name))
                .ObjectId;

        private IEnumerable<long> GetGarbageCollectable() => _storageEngine.GarbageCollectableIds;

      /*  private class StorageEngine : IStorageEngine
        {
            private readonly object _sync = new object();
            private IEnumerable<StorageEntry> _entries = Enumerable.Empty<StorageEntry>();
            private IEnumerable<long> _garbageCollectables = Enumerable.Empty<long>();

            public void Persist(DetectedChanges detectedChanges)
            {
                lock (_sync)
                {
                    _entries = _entries.Concat(detectedChanges.NewEntries);
                    _garbageCollectables = _garbageCollectables.Concat(detectedChanges.GarbageCollectableIds);
                }
            }

            public StoredState Load()
            {
                throw new System.NotImplementedException();
            }

            public IEnumerable<StorageEntry> LoadAllEntries()
            {
                lock (_sync)
                    return _entries.ToList();
            }

            public IEnumerable<long> GarbageCollectables
            {
                get
                {
                    lock (_sync)
                        return _garbageCollectables.ToList();
                }
            }

            public void Dispose() { }
        }*/
    }
}