using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.PersistentDataStructures;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ObjectStore.PersistentDataStructures
{
    [TestClass]
    public class CListTests
    {
        [TestMethod]
        public void AddAndRemoveNonReferencingElementsFromCList()
        {
            var storage = new InMemoryStorageEngine();
            var os = ObjectStore2.New(storage);

            var cList = new CList<string>();
            cList.Add("hello");
            cList.Add("world");
            cList.Add("from here");
            
            os.Roots.Entangle(cList);
            os.Persist();

            os = ObjectStore2.Load(storage);
            cList = os.Roots.Find<CList<string>>();
            cList.Count.ShouldBe(3);
            cList[0].ShouldBe("hello");
            cList[1].ShouldBe("world");
            cList[2].ShouldBe("from here");

            cList.Remove(0);
            
            os.Persist();
            
            os = ObjectStore2.Load(storage);
            cList = os.Roots.Find<CList<string>>();
            cList.Count.ShouldBe(2);
            cList[0].ShouldBe("world");
            cList[1].ShouldBe("from here");
            cList.Remove(1);

            os.Persist();
            
            os = ObjectStore2.Load(storage);
            cList = os.Roots.Find<CList<string>>();
            cList.Count.ShouldBe(1);
            cList[0].ShouldBe("world");
            cList.Add("hello");
            
            os.Persist();
            
            os = ObjectStore2.Load(storage);
            cList = os.Roots.Find<CList<string>>();
            cList.Count.ShouldBe(2);
            cList[0].ShouldBe("world");
            cList[1].ShouldBe("hello");
            
            cList.Remove(0);
            cList.Remove(0);
            
            os.Persist();
            
            os = ObjectStore2.Load(storage);
            cList = os.Roots.Find<CList<string>>();
            cList.Count.ShouldBe(0);
        }
    }
}