using System.Collections.Generic;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Api;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Roots = Cleipnir.ExecutionEngine.Api.Roots;

namespace Cleipnir.Tests.SchedulerTests
{
    [TestClass]
    public class SleepTest
    {
        private InMemoryStorageEngine StableStorageEngine { get; set; }
        private Engine Scheduler { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            StableStorageEngine = new InMemoryStorageEngine();
            Scheduler = ExecutionEngine.ExecutionEngineFactory.StartNew(StableStorageEngine);
        }

        [TestCleanup]
        public void CleanUp() => Scheduler.Dispose();

        private void LoadAgain() => Scheduler = ExecutionEngine.ExecutionEngineFactory.Continue(StableStorageEngine);

        [TestMethod]
        public async Task CreateAndWaitForPersistentSleep()
        {
            var workflow = new Workflow( true);

            await Scheduler.Entangle(workflow);
            _ = Scheduler.Schedule(() => _ = workflow.Do());
            await Task.Delay(100);
            Scheduler.Dispose();

            workflow.IsCompleted().ShouldBeFalse();

            LoadAgain();
            await Task.Delay(2000);
            var isCompleted = await Scheduler.Schedule(() => Roots.Resolve<Workflow>().IsCompleted());
            
            isCompleted.ShouldBeTrue();
        }

        [TestMethod]
        public async Task CreateAndWaitForNonPersistentSleep()
        {
            var workflow = new Workflow( false);

            await Scheduler.Entangle(workflow);
            _ = Scheduler.Schedule(() => _ = workflow.Do());
            await Task.Delay(100);
            Scheduler.Dispose();

            workflow.IsCompleted().ShouldBeFalse();

            LoadAgain();

            await Task.Delay(2000);

            var isCompleted = await Scheduler.Schedule(() => Roots.Resolve<Workflow>().IsCompleted());

            isCompleted.ShouldBeFalse();
        }

        [TestMethod]
        public async Task CreateAndWaitForNonPersistentSleepThatCompletesBeforeSync()
        {
            var workflow = new Workflow( false);

            await Scheduler.Entangle(workflow);
            _ = Scheduler.Schedule(() => _ = workflow.Do());
            await Task.Delay(2000);
            
            workflow.IsCompleted().ShouldBeTrue();

            Scheduler.Dispose();

            LoadAgain();
            await Task.Delay(2000);

            var isCompleted = await Scheduler.Schedule(() => Roots.Resolve<Workflow>().IsCompleted());
            isCompleted.ShouldBeFalse();
        }

        private class Workflow : IPersistable
        {
            private volatile bool _isCompleted = false;
            private readonly bool _isPersistent;

            public Workflow(bool isPersistent)
            {
                _isPersistent = isPersistent;
            }

            public async CTask Do()
            {
                await Sleep.Until(1000, _isPersistent);
                _isCompleted = true;
            }

            public bool IsCompleted() => _isCompleted;

            public void Serialize(StateMap sd, SerializationHelper helper)
                => sd.Set(nameof(_isPersistent), _isPersistent);

            private static Workflow Deserialize(IReadOnlyDictionary<string, object> sd) 
                => new Workflow((bool) sd[nameof(_isPersistent)]);
        }
    }
}
