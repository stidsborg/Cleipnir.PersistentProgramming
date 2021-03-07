using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Api;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.Rx.ExecutionEngine;
using Cleipnir.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Roots = Cleipnir.ExecutionEngine.Api.Roots;

namespace Cleipnir.Tests.ReactiveTests
{
    [TestClass]
    public class WaitForOperatorWithRestartTests
    {

        [TestMethod]
        public void WaitForOperatorEmitsElementsBeforeTimeOutDespiteRestart()
        {
            var facade = new TestFacade();

            var synced = new Synced<WaitForWorkflow>();

            facade.Schedule(() =>
            {
                var workflow = new WaitForWorkflow(5000);

                Sync.AfterNext(workflow.EmitEventsToSource, true);
                Sync.AfterNext(workflow.SetFieldVariableAfterWaitedFor, true);
            });

            Thread.Sleep(100);

            facade.PersistAndCloseDown();

            Thread.Sleep(100);

            facade.LoadAgain();

            Thread.Sleep(3000);

            synced.Value.ShouldBeNull();

            var w = facade.Resolve<WaitForWorkflow>();
            w.Emits.ToArray().Length.ShouldBe(5);

            facade.Dispose();
        }

        [TestMethod]
        public void WaitForOperatorEmitsElementsThrowsExceptionDespiteRestart()
        {
            var facade = new TestFacade();

            var synced = new Synced<Exception>();

            facade.Schedule(() =>
            {
                var workflow = new WaitForWorkflow(1000);
                Sync.AfterNext(workflow.EmitEventsToSource, true);
                Sync.AfterNext(workflow.SetFieldVariableAfterWaitedFor, true);
            });

            Thread.Sleep(100);

            facade.PersistAndCloseDown();

            Thread.Sleep(100);

            facade.LoadAgain();

            Thread.Sleep(3000);

            synced.Value.ShouldBeNull();

            var thrownException = facade.Resolve<WaitForWorkflow>().ThrownException;
            (thrownException is TimeoutException).ShouldBeTrue();
            
            facade.Dispose();
        }

        private class WaitForWorkflow : IPersistable
        {
            public WaitForWorkflow(int waitForTimeout)
            {
                Source = new Source<int>();
                WaitFor = Source.WaitFor(5, true, TimeSpan.FromMilliseconds(waitForTimeout));
                Roots.Entangle(this);
            }

            private WaitForWorkflow(Source<int> source, CAwaitable<IEnumerable<int>> waitFor, Exception thrownException)
            {
                Source = source;
                WaitFor = waitFor;
                ThrownException = thrownException;
            }

            private Source<int> Source { get; }
            private CAwaitable<IEnumerable<int>> WaitFor { get; }

            public Exception ThrownException { get; private set; }
            public IEnumerable<int> Emits { get; private set; }

            public void EmitEventsToSource()
            {
                async CTask Do()
                {
                    for (var i = 0; i < 5; i++)
                    {
                        await Sleep.Until(500, true);
                        Source.Emit(i);
                    }
                }
                _ = Do();
            }

            public void SetFieldVariableAfterWaitedFor()
            {
                async CTask Do()
                {
                    try
                    {
                        Emits = await WaitFor;
                    }
                    catch (Exception e)
                    {
                        ThrownException = e;
                    }
                }

                _ = Do();
            }

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(Source), Source);
                sd.Set(nameof(WaitFor), WaitFor);
                sd.Set(nameof(ThrownException), ThrownException);
            }

            private static WaitForWorkflow Deserialize(IReadOnlyDictionary<string, object> sd)
                => new WaitForWorkflow(
                    sd.Get<Source<int>>(nameof(Source)),
                    sd.Get<CAwaitable<IEnumerable<int>>>(nameof(WaitFor)),
                    sd.Get<Exception>(nameof(ThrownException))
                );
        }

        [TestMethod]
        public async Task SimpleWaitForWithTimeoutTest()
        {
            var facade = new TestFacade();

            _ = facade.Schedule(() =>
            {
                var w = new SimpleWaitForWorkflow();
                Roots.Entangle(w);
                _  = w.Start();
            });

            await facade.Scheduler.Sync();

            var workflow = facade.Resolve<SimpleWaitForWorkflow>();
            workflow.ThrownException.ShouldBeNull();
            facade.LoadAgain();

            await Task.Delay(3000);

            workflow = facade.Resolve<SimpleWaitForWorkflow>();
            workflow.ThrownException.ShouldNotBeNull();

            facade.Dispose();
        }

        private class SimpleWaitForWorkflow : IPersistable
        {
            public Exception ThrownException { get; set; }
            public Source<int> Source { get; set; }

            public async CTask Start()
            {
                var source = new Source<int>();
                Source = source;
                try
                {
                    var elms = await source.WaitFor(5, true, TimeSpan.FromSeconds(2));
                }
                catch (Exception e)
                {
                    ThrownException = e;
                }
            }

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(ThrownException), ThrownException);
                sd.Set(nameof(Source), Source);
            }

            private static SimpleWaitForWorkflow Deserialize(IReadOnlyDictionary<string, object> sd)
                => new SimpleWaitForWorkflow()
                {
                    ThrownException = sd.Get<Exception>(nameof(ThrownException)),
                    Source = sd.Get<Source<int>>(nameof(Source))
                };
        }
    }
}
