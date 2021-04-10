using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers;
using Cleipnir.ObjectDB.Version2.TaskAndAwaitable.Awaitables;
using Cleipnir.ObjectDB.Version2.TaskAndAwaitable.StateMachine;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ObjectStore.Persistence
{
    [TestClass]
    public class TasksAndAwaitableTests
    {
        [TestMethod]
        public void SerializeAndDeserializeTaskAndAwaitableSuccessfully()
        {
            var storage = new InMemoryStorageEngine();
            var os = ObjectStore2.New(storage);

            var awaitable = new CAwaitable2<string>();
            var wrapper = new Wrapper();
            
            os.Roots.Entangle(awaitable);
            os.Roots.Entangle(wrapper);

            _ = wrapper.Do(awaitable);
            
            os.Persist();
            
            os = ObjectStore2.Load(storage);
            awaitable = os.Roots.Find<CAwaitable2<string>>();
            wrapper = os.Roots.Find<Wrapper>();
            
            wrapper.State.ShouldBeNull();
            awaitable.SignalCompletion("hello world");
            
            wrapper.State.ShouldBe("hello world");
        }

        private class Wrapper : IPersistable2
        {
            public string State { get; private set; }

            public async CTask2 Do(CAwaitable2<string> awaitable) => State = await awaitable;

            public void Serialize(Map2 m)
                => m[nameof(State)] = State;

            private static Wrapper Deserialize(RMap rm) 
                => new Wrapper() { State = rm.Get<string>(nameof(State)) };
        }
    }
}