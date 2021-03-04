using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB;
using Cleipnir.StorageEngine.InMemory;
using Cleipnir.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Cleipnir.Tests.SchedulerTests
{
    [TestClass]
    public class ExecutionEngineContextTests
    {
        [TestMethod]
        public void DelegatesAndSchedulerAreSetAfterStartingTheScheduler()
        {
            var objectStore = ObjectStore.New(new InMemoryStorageEngine());
            var syncs = new SynchronizationQueue();
            var scheduler = new InternalScheduler(objectStore, new ReadyToSchedules(), syncs, new Engine(), false);

            scheduler.Start();

            var synced = new Synced<bool>();

            scheduler.Schedule(() => synced.Value = Roots.Instance.Value == objectStore.Roots, false);

            synced.WaitFor(d => synced.Value);

            scheduler.Dispose();
        }
    }
}
