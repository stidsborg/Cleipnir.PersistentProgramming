using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Api;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.StorageEngine.InMemory;
using Cleipnir.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Roots = Cleipnir.ExecutionEngine.Api.Roots;

namespace Cleipnir.Tests.SchedulerTests
{
    [TestClass]
    public class WorkflowTests
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

        [TestMethod]
        public void StartWorkflowOneWithDelayAndWaitForCompletion()
        {
            Scheduler.Schedule(() =>
            {
                var w = new WorkflowOne();
                Roots.Entangle(w);
                _ = w.Do();
            });
            
            Thread.Sleep(1000);
            var workflow = Scheduler.Resolve<WorkflowOne>().Result;
            Scheduler.Dispose();

            workflow.Status.ShouldBe(-1);

            var facade = ExecutionEngine.ExecutionEngineFactory.Continue(StableStorageEngine);

            Thread.Sleep(1500);

            var loadedWorkflow = facade.Resolve<WorkflowOne>().Result;

            loadedWorkflow.Status.ShouldBe(1);
            workflow.Status.ShouldBe(-1);

            facade.Dispose();
        }

        private class WorkflowOne : IPersistable
        {
            public int Status { get; set; }

            public async CTask Do()
            {
                Status = -1;
                await Sync.Next();
                await Sleep.Until(2000, true);
                Status = 1;
            }

            public void Serialize(StateMap sd, SerializationHelper helper) => sd.Set(nameof(Status), Status);

            private static WorkflowOne Deserialize(IReadOnlyDictionary<string, object> sd) 
                => new WorkflowOne { Status = (int) sd[nameof(Status)] };
        }


        [TestMethod]
        public async Task StartWorkflowTwoWithDelayAndWaitForCompletion()
        {
            await Scheduler.ScheduleTask(() =>
            {
                var workflow = new WorkflowTwo { Status = 100 };
                Roots.Entangle(workflow);
                return Sync.Next(false);
            });
            
            var scheduler = ExecutionEngine.ExecutionEngineFactory.Continue(StableStorageEngine);

            var status = await Scheduler.Schedule(() => Roots.Resolve<WorkflowTwo>().Status);

            status.ShouldBe(100);
            
            scheduler.Dispose();
        }

        private class WorkflowTwo : IPersistable
        {
            public int Status { get; set; }

            public async CTask Do()
            {
                Status = -1;
                await Sleep.Until(2000, true);
                Status = 1;
            }

            public void Serialize(StateMap sd, SerializationHelper helper) => sd.Set(nameof(Status), Status);

            private static WorkflowTwo Deserialize(IReadOnlyDictionary<string, object> sd)
                => new WorkflowTwo { Status = (int) sd[nameof(Status)] };
        }
    }
}
