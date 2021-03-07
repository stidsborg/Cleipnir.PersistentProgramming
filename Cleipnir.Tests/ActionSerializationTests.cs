using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.Persistency.Persistency;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Roots = Cleipnir.ExecutionEngine.Api.Roots;

namespace Cleipnir.Tests
{
    [TestClass]
    public class ActionSerializationTests
    {
        private InMemoryStorageEngine StableStorageEngine { get; set; }
        private Engine Scheduler { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            StableStorageEngine = new InMemoryStorageEngine();
            Scheduler = ExecutionEngineFactory.StartNew(StableStorageEngine);
        }

        [TestMethod]
        public async Task SerializeAndDeserializeAction()
        {
            {
                var person = new Person1();
                await Scheduler.Entangle(person);
            }

            LoadAgain();
            
            {
                var result = await Scheduler.Schedule(() =>
                {
                    var person = Roots.Resolve<Person1>();
                    person.SetAction();
                    return person.InvokedValue;
                });
                result.ShouldBeNull();
            }

            LoadAgain();
            
            {
                var result = await Scheduler.Schedule(() =>
                {
                    var person = Roots.Resolve<Person1>();
                    person.InvokeAction("Parameter");
                    return person.InvokedValue;
                });
                result.ShouldBe("Parameter");
            }
        }

        private class Person1 : IPersistable
        {
            private Action<string> _action;
            public string InvokedValue { get; private set; }

            public void InvokeAction(string name) => _action(name);
            public void SetAction() => _action = Method;

            private void Method(string value) => InvokedValue = value;

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(_action), helper.GetReference(_action));
                sd.Set(nameof(InvokedValue), InvokedValue);
            }

            private static Person1 Deserialize(IReadOnlyDictionary<string, object> sd)
            {
                var person = new Person1
                {
                    InvokedValue = sd.Get<string>(nameof(InvokedValue))
                };
                sd.Get<Reference>(nameof(_action)).DoWhenResolved<Action<string>>(a => person._action = a);

                return person;
            }
        }

        [TestMethod]
        public async Task SerializeAndDeserializeActionCastedToObject()
        {
            var person = new Person();
            Action<string> action = person.SayHello;
            await Scheduler.Entangle(action);

            Scheduler.Dispose();

            LoadAgain();

            await Scheduler.Schedule(() => Roots.Resolve<Action<string>>());
        }

        private class Person : IPersistable
        {
            public string InvokedValue { get; private set; }
            public void SayHello(string name) => InvokedValue = name;

            public void Serialize(StateMap sd, SerializationHelper helper) => sd.Set(nameof(InvokedValue), InvokedValue);
            private static Person Deserialize(IReadOnlyDictionary<string, object> sd)
                => new Person {InvokedValue = sd.Get<string>(nameof(InvokedValue))};
        }

        private void LoadAgain()
        {
            Scheduler.Dispose();

            Scheduler = ExecutionEngine.ExecutionEngineFactory.Continue(StableStorageEngine);
        }
    }
}
