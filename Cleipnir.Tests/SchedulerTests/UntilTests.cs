using System.Collections.Generic;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Api;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
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
    public class UntilTests
    {
        [TestMethod]
        public void YieldWorksTest()
        {
            var executionEngine = ExecutionEngineFactory.StartNew(new InMemoryStorageEngine());
            YieldTestHolder holder = null;
            var task = executionEngine.Schedule(() =>
            {
                holder = new YieldTestHolder();
                return holder.Start();
            });
            task.Wait();
            var yieldCompleted = executionEngine.Schedule(() => holder.YieldCompleted).Result;
            yieldCompleted.ShouldBe(true);
        }

        private class YieldTestHolder
        {
            public bool YieldCompleted { get; set; }
            
            public async CTask Start()
            {
                await Scheduler.Yield();
                YieldCompleted = true;
            }
        }

        [TestMethod]
        public void UntilWorksTest()
        {
            var storage = new InMemoryStorageEngine();
            var executionEngine = ExecutionEngineFactory.StartNew(storage);
            
            executionEngine.Schedule(() =>
            {
                var holder = new UntilTestHolder();
                var task = holder.Start();
                Roots.Entangle(holder);
                Roots.Entangle(task);
                return Sync.Next();
            }).Wait();
           
            executionEngine.Dispose();

            executionEngine = ExecutionEngineFactory.Continue(storage);

            executionEngine.Schedule(() =>
            {
                var holder = Roots.Resolve<UntilTestHolder>();
                holder.Stop = true;
                return Roots.Resolve<CTask>();
            }).Wait();

            var completed = executionEngine.Schedule(() => Roots.Resolve<UntilTestHolder>().Completed).Result;

            completed.ShouldBe(true);
        }

        private class UntilTestHolder : IPersistable
        {
            public bool Completed { get; private set; }
            public bool Stop { private get; set; }

            public async CTask Start()
            {
                await Until.This(ShouldStop, true);
                Completed = true;
            }

            public bool ShouldStop() => Stop;

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(Completed), Completed);
                sd.Set(nameof(Stop), Stop);
            }

            private static UntilTestHolder Deserialize(IReadOnlyDictionary<string, object> sd) 
                => new UntilTestHolder
                {
                    Completed = sd.Get<bool>(nameof(Completed)),
                    Stop = sd.Get<bool>(nameof(Stop))
                };
        }
    }
}
