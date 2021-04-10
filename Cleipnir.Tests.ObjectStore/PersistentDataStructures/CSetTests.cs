using System.Linq;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.PersistentDataStructures;
using Cleipnir.StorageEngine.InMemory;
using Cleipnir.Tests.ObjectStore.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ObjectStore.PersistentDataStructures
{
    [TestClass]
    public class CSetTests
    {
        [TestMethod]
        public void AddAndRemovedEntriesFromDictionary()
        {
            var storage = new InMemoryStorageEngine();
            var os = ObjectStore2.New(storage);
            var s = new CSet<Person>();
            os.Roots.Entangle(s);

            var peter = new Person {Name = "Peter"};
            var ole = new Person {Name = "Ole", Relationship = peter};
            s.Add(peter);
            s.Add(ole);

            os.Persist();
            
            os = ObjectStore2.Load(storage);
            s = os.Roots.Find<CSet<Person>>();
            
            s.Count.ShouldBe(2);
            peter = s.Single(p => p.Name == "Peter");
            ole = s.Single(p => p.Name == "Ole");
            
            ole.Relationship.ShouldBe(peter);
            
            s.Remove(ole);
            os.Persist();
            
            os = ObjectStore2.Load(storage);
            s = os.Roots.Find<CSet<Person>>();
            
            s.Count.ShouldBe(1);
            peter = s.Single(p => p.Name == "Peter");
            s.Add(peter);
            
            s.Count.ShouldBe(1);
            
            os.Persist();
            os = ObjectStore2.Load(storage);
            s = os.Roots.Find<CSet<Person>>();
            
            s.Count.ShouldBe(1);
            peter = s.Single(p => p.Name == "Peter");
            s.Remove(peter);
            
            os.Persist();
            
            os = ObjectStore2.Load(storage);
            s = os.Roots.Find<CSet<Person>>();
            s.Count.ShouldBe(0);
        }
    }
}