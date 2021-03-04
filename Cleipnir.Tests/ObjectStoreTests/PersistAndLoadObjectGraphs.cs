using System.Collections.Generic;
using Cleipnir.ObjectDB;
using Cleipnir.StorageEngine;
using Cleipnir.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ObjectStoreTests
{
    [TestClass]
    public class PersistAndLoadObjectGraphs
    {
        [TestMethod]
        public void StorageEngineIsNotCalledWhenNoChangesHaveBeenObserved()
        {
            var storageEngine = new StorageEngine();
            var os = ObjectStore.New(storageEngine);
            
            os.Entangle("hello world");
            storageEngine.InvokedCount.Value.ShouldBe(0);
            os.Persist();
            storageEngine.InvokedCount.Value.ShouldBe(1);
            os.Persist();
            storageEngine.InvokedCount.Value.ShouldBe(1);
        }

        private class StorageEngine : IStorageEngine
        {
            public Synced<int> InvokedCount { get; } = new Synced<int>();

            public void Persist(DetectedChanges detectedChanges)
            {
                InvokedCount.Value++;
            }

            public StoredState Load()
            {
                throw new System.NotImplementedException();
            }

            public void Dispose() { }
        }
    }
}