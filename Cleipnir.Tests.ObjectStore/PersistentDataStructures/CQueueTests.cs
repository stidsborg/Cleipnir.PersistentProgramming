using System.Linq;
using System.Xml.Linq;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.PersistentDataStructures;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ObjectStore.PersistentDataStructures
{
    [TestClass]
    public class CQueueTests
    {
        [TestMethod]
        public void EnqueueAndDequeueFromCQueue()
        {
            var storage = new InMemoryStorageEngine();
            var os = ObjectStore2.New(storage);

            var cQueue = new CQueue<string>();
            cQueue.Enqueue("hello");
            cQueue.Enqueue("world");
            cQueue.Enqueue("from here");
            
            os.Roots.Entangle(cQueue);
            
            os.Persist();
            os = ObjectStore2.Load(storage);
            cQueue = os.Roots.Find<CQueue<string>>();
            
            cQueue.Count.ShouldBe(3);

            var dequeued = cQueue.Dequeue();
            dequeued.ShouldBe("hello");

            os.Persist();
            
            
            os = ObjectStore2.Load(storage);
            cQueue = os.Roots.Find<CQueue<string>>();
            
            cQueue.Count.ShouldBe(2);
            
            cQueue.Dequeue().ShouldBe("world");
            cQueue.Dequeue().ShouldBe("from here");
            
            cQueue.Enqueue("hello");
            
            os.Persist();
            
            os = ObjectStore2.Load(storage);
            cQueue = os.Roots.Find<CQueue<string>>();
            
            cQueue.Count.ShouldBe(1);
            cQueue.Dequeue().ShouldBe("hello");
            
            os.Persist();
            
            os = ObjectStore2.Load(storage);
            cQueue = os.Roots.Find<CQueue<string>>();
            cQueue.Count.ShouldBe(0);
        }
    }
}