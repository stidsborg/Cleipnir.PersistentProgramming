using System.Linq;
using Cleipnir.ExecutionEngine.Api;
using Cleipnir.ObjectDB;
using Cleipnir.StorageEngine;

namespace Cleipnir.ExecutionEngine
{
    public static class ExecutionEngineFactory
    {
        public static Engine Continue(IStorageEngine storageEngine, params object[] ephemeralInstances)
            => Continue(storageEngine, false, ephemeralInstances);

        public static Engine Continue(IStorageEngine storageEngine, bool detectCircularDependencies, params object[] ephemeralInstances)
        {
            var syncs = new SynchronizationQueue();

            var proxyScheduler = new ProxyScheduler();
            Scheduler.ThreadLocalScheduler.Value = proxyScheduler; //make the scheduler available to deserializers

            var engineScheduler = new Engine {Scheduler = proxyScheduler};
            Engine._current.Value = engineScheduler;

            var frameworkEphemeralInstances = new object[] { proxyScheduler, engineScheduler };

            var userAndFrameworkEphemeralInstances = ephemeralInstances.Concat(frameworkEphemeralInstances).ToArray();
            var store = ObjectStore.Load(storageEngine, userAndFrameworkEphemeralInstances);

            var readyToSchedules = store.Resolve<ReadyToSchedules>();

            var scheduler = new InternalScheduler(store, readyToSchedules, syncs, engineScheduler, detectCircularDependencies);
            engineScheduler.Scheduler = scheduler;

            proxyScheduler.Scheduler = scheduler;

            scheduler.Start();

            return engineScheduler;
        }

        public static Engine StartNew(IStorageEngine storageEngine)
            => StartNew(storageEngine, false);
        
        public static Engine StartNew(IStorageEngine storageEngine, bool detectCircularDependencies)
        {
            var syncs = new SynchronizationQueue();

            var objectStore = ObjectStore.New(storageEngine);

            var readyToSchedules = new ReadyToSchedules();
            
            var engineScheduler = new Engine();
            var scheduler = new InternalScheduler(objectStore, readyToSchedules, syncs, engineScheduler, detectCircularDependencies);
            engineScheduler.Scheduler = scheduler;

            objectStore.Entangle(readyToSchedules);

            scheduler.Start();

            return engineScheduler;
        }
    }
}
