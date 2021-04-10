using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers;
using Cleipnir.ObjectDB.Version2.PersistentDataStructures;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ObjectStore.PersistentDataStructures
{
    [TestClass]
    public class ChangeTrackerTests
    {
        [TestMethod]
        public void ChangeTrackerWithNoChangesCanBePersistedAndLoadedAgain()
        {
            var storage = new InMemoryStorageEngine();
            var os = ObjectStore2.New(storage);

            var ctw = new ChangeTrackerWrapper();
            os.Roots.Entangle(ctw);
            
            os.Persist();
            
            os = ObjectStore2.Load(storage);
            ctw = os.Roots.Find<ChangeTrackerWrapper>();
            
            ctw.ValuesFromHead().Count().ShouldBe(0);
            ctw.ValuesFromTail().Count().ShouldBe(0);
        }
        
        [TestMethod]
        public void ChangeTrackerWithSetNodeValueSurvivesPersistence()
        {
            var storage = new InMemoryStorageEngine();
            var os = ObjectStore2.New(storage);

            var ctw = new ChangeTrackerWrapper();
            os.Roots.Entangle(ctw);

            ctw.ChangeTracker.Append(1);
            ctw.ChangeTracker.Append(2);
            os.Persist();
            
            os = ObjectStore2.Load(storage);
            ctw = os.Roots.Find<ChangeTrackerWrapper>();

            ctw.ChangeTracker.Head.Value = 3;
            os.Persist();

            os = ObjectStore2.Load(storage);
            ctw = os.Roots.Find<ChangeTrackerWrapper>();
            ctw.ValuesFromHead().SequenceEqual(new[] {3, 2}).ShouldBeTrue();
        }
        
        [TestMethod]
        public void ChangeTrackerWithNodeChangedAndRemovedBeforePersisted()
        {
            var storage = new InMemoryStorageEngine();
            var os = ObjectStore2.New(storage);

            var ctw = new ChangeTrackerWrapper();
            os.Roots.Entangle(ctw);

            ctw.ChangeTracker.Append(1);
            ctw.ChangeTracker.Append(2);
            ctw.ChangeTracker.Head.Remove();

            os.Persist();

            os = ObjectStore2.Load(storage);
            ctw = os.Roots.Find<ChangeTrackerWrapper>();
            ctw.ChangeTracker.Head.Value.ShouldBe(2);
            ctw.ChangeTracker.Tail.Value.ShouldBe(2);
        }
        
        [TestMethod]
        public void ChangeTrackerWithAddedElementsRemovedOverSeveralSynchronizations()
        {
            var storage = new InMemoryStorageEngine();
            var os = ObjectStore2.New(storage);

            var ctw = new ChangeTrackerWrapper();
            os.Roots.Entangle(ctw);

            ctw.ChangeTracker.Append(1);
            ctw.ChangeTracker.Append(2);
            ctw.ChangeTracker.Append(3);
            ctw.ChangeTracker.Prepend(0);

            os.Persist();
            
            os = ObjectStore2.Load(storage);
            ctw = os.Roots.Find<ChangeTrackerWrapper>();

            ctw.ValuesFromHead().SequenceEqual(new[] {0, 1, 2, 3}).ShouldBeTrue();
            ctw.ValuesFromTail().SequenceEqual(new[] {3, 2, 1, 0}).ShouldBeTrue();
            
            ctw.ChangeTracker.Head.Next.Remove();
            
            os.Persist();
            
            os = ObjectStore2.Load(storage);
            ctw = os.Roots.Find<ChangeTrackerWrapper>();

            ctw.ValuesFromHead().SequenceEqual(new[] {0, 2, 3}).ShouldBeTrue();
            ctw.ValuesFromTail().SequenceEqual(new[] {3, 2, 0}).ShouldBeTrue();
        }

        private class ChangeTrackerWrapper : IPersistable2
        {
            public ChangeTracker<int> ChangeTracker { get; private set; } = new();

            public IEnumerable<int> ValuesFromHead()
            {
                var curr = ChangeTracker.Head;
                while (curr != null)
                {
                    yield return curr.Value;
                    curr = curr.Next;
                }
            }
            
            public IEnumerable<int> ValuesFromTail()
            {
                var curr = ChangeTracker.Tail;
                while (curr != null)
                {
                    yield return curr.Value;
                    curr = curr.Prev;
                }
            }
            
            public void Serialize(Map2 m) => ChangeTracker.Serialize(m);

            private static ChangeTrackerWrapper Deserialize(RMap rm) 
                => new ChangeTrackerWrapper() {ChangeTracker = ChangeTracker<int>.Deserialize(rm)};
        }
    }
}