using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.PersistentDataStructures;
using Cleipnir.StorageEngine.InMemory;
using Cleipnir.Tests.ObjectStore.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ObjectStore.PersistentDataStructures
{
    [TestClass]
    public class CDictionaryTests
    {
        [TestMethod]
        public void AddAndRemovedEntriesFromDictionary()
        {
            var storage = new InMemoryStorageEngine();
            var os = ObjectStore2.New(storage);
            var d = new CDictionary<string, Person>();
            os.Roots.Entangle(d);
            
            d["101020"] = new Person {Name = "Peter"};
            d["311209"] = new Person {Name = "Ole", Relationship = d["101020"]};
            
            os.Persist();
            
            os = ObjectStore2.Load(storage);
            d = os.Roots.Find<CDictionary<string, Person>>();
            
            d.Count.ShouldBe(2);
            d["101020"].Name.ShouldBe("Peter");
            d["101020"].Relationship.ShouldBeNull();

            d["311209"].Name.ShouldBe("Ole");
            d["311209"].Relationship.Name.ShouldBe("Peter");
            
            d.Remove("311209");
            
            os.Persist();
            os = ObjectStore2.Load(storage);
            d = os.Roots.Find<CDictionary<string, Person>>();
            
            d.Count.ShouldBe(1);
            d["101020"].Name.ShouldBe("Peter");
            d["101020"].Relationship.ShouldBeNull();
            d["311209"] = d["101020"];
            
            os.Persist();
            os = ObjectStore2.Load(storage);
            d = os.Roots.Find<CDictionary<string, Person>>();
            
            d.Count.ShouldBe(2);
            d["101020"].ShouldBe(d["311209"]);
            
            d.Remove("101020");
            d.Remove("311209");
            
            os.Persist();
            os = ObjectStore2.Load(storage);
            d = os.Roots.Find<CDictionary<string, Person>>();
            
            d.Count.ShouldBe(0);
        }

    }
}