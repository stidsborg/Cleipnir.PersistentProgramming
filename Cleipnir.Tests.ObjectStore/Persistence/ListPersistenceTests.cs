using System.Collections.Generic;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ObjectStore.Persistence
{
    [TestClass]
    public class ListPersistenceTests
    {
        [TestMethod]
        public void AddAndRemoveElementsFromList()
        {
            var storage = new InMemoryStorageEngine();
            var os = ObjectStore2.New(storage);
            var wrapper = new Wrapper();
            os.Roots.Entangle(wrapper);
            
            os.Persist();
            os = ObjectStore2.Load(storage);
            wrapper = os.Roots.Find<Wrapper>();
            
            wrapper.List.Count.ShouldBe(0);
            
            wrapper.List.Add("hello");
            wrapper.List.Add("world");
            
            os.Persist();
            os = ObjectStore2.Load(storage);
            wrapper = os.Roots.Find<Wrapper>();
            
            wrapper.List.Count.ShouldBe(2);
            wrapper.List[0].ShouldBe("hello");
            wrapper.List[1].ShouldBe("world");
            
            wrapper.List.RemoveAt(0);
            
            os.Persist();
            os = ObjectStore2.Load(storage);
            wrapper = os.Roots.Find<Wrapper>();
            
            wrapper.List.Count.ShouldBe(1);
            wrapper.List[0].ShouldBe("world");
            
            wrapper.List.Clear();
            
            os.Persist();
            os = ObjectStore2.Load(storage);
            wrapper = os.Roots.Find<Wrapper>();
            
            wrapper.List.Count.ShouldBe(0);
        }
        
        private class Wrapper : IPersistable2
        {
            public List<string> List { get; }

            public Wrapper() => List = new List<string>();
            private Wrapper(List<string> l) => List = l;

            public void Serialize(Map2 m) => m[nameof(List)] = List;

            private static Wrapper Deserialize(RMap rm)
                => new Wrapper(rm.Get<List<string>>(nameof(List)));
        }
    }
}