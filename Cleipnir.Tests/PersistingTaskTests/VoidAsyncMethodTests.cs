using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.StorageEngine.InMemory;
using Cleipnir.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.PersistingTaskTests
{
    [TestClass]
    public class VoidAsyncMethodTests
    {
        [TestMethod]
        public void AsyncVoidMethodCanBeSerialized()
        {
            var storage = new InMemoryStorageEngine();
            var os = ObjectStore.New(storage);

            var a1 = new CAwaitable();
            var a2 = new CAwaitable();
            var tuple = new PTuple<CAwaitable, CAwaitable>(a1, a2);
            var value = new PValue<int>();
            
            AsyncMethodTest(a1, a2, value);

            os.Entangle(tuple);
            os.Entangle(value);
            os.Persist();

            os = ObjectStore.Load(storage, true);
            tuple = os.Resolve<PTuple<CAwaitable, CAwaitable>>();
            value = os.Resolve<PValue<int>>();

            value.Value.ShouldBe(-1);

            tuple.First.SignalCompletion();
            value.Value.ShouldBe(1);

            os.Persist();

            os = ObjectStore.Load(storage, true);
            tuple = os.Resolve<PTuple<CAwaitable, CAwaitable>>();
            value = os.Resolve<PValue<int>>();

            value.Value.ShouldBe(1);

            tuple.Second.SignalCompletion();

            value.Value.ShouldBe(2);

            os.Persist();

            os = ObjectStore.Load(storage, true);
            tuple = os.Resolve<PTuple<CAwaitable, CAwaitable>>();
            value = os.Resolve<PValue<int>>();

            value.Value.ShouldBe(2);
        }

        private void AsyncMethodTest(CAwaitable a1, CAwaitable a2, PValue<int> value)
        {
            async CTask Do()
            {
                value.Value = -1;
                await a1;
                value.Value = 1;
                await a2;
                value.Value = 2;
            }

            _ = Do();
        }
    }
}
