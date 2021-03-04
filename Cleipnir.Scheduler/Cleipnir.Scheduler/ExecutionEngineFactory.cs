using System.Linq;
using Cleipnir.ObjectDB;
using Cleipnir.StorageEngine;

namespace Cleipnir.ExecutionEngine
{
    public static class ExecutionEngineFactory
    {
        public static Engine Continue(IStorageEngine storageEngine, params object[] ephemeralInstances)
        {
            var syncs = new SynchronizationQueue();

            var proxyScheduler = new ProxyScheduler();
            Scheduler.ThreadLocalScheduler.Value = proxyScheduler; //make the scheduler available to deserializers

            var engineScheduler = new Engine {Scheduler = proxyScheduler};
            Engine._current.Value = engineScheduler;

            var frameworkEphemeralInstances = new object[] { proxyScheduler, engineScheduler };

            var concatList = ephemeralInstances.Concat(frameworkEphemeralInstances).ToArray();
            var store = ObjectStore.Load(storageEngine, concatList);

            var readyToSchedules = store.Resolve<ReadyToSchedules>();

            var scheduler = new InternalScheduler(store, readyToSchedules, syncs, engineScheduler);
            engineScheduler.Scheduler = scheduler;

            proxyScheduler.Scheduler = scheduler;

            scheduler.Start();

            return engineScheduler;
        }

        public static Engine StartNew(IStorageEngine storageEngine)
        {
            var syncs = new SynchronizationQueue();

            var objectStore = ObjectStore.New(storageEngine);

            var readyToSchedules = new ReadyToSchedules();
            
            var engineScheduler = new Engine();
            var scheduler = new InternalScheduler(objectStore, readyToSchedules, syncs, engineScheduler);
            engineScheduler.Scheduler = scheduler;

            objectStore.Entangle(readyToSchedules);

            scheduler.Start();

            return engineScheduler;
        }
    }
}
