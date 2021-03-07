using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.Persistency.Persistency;
using Cleipnir.StorageEngine.InMemory;
using Cleipnir.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests
{
    [TestClass]
    public class ReferenceTests
    {
        [TestInitialize]
        public void Initialize()
        {
            StorageEngine = new InMemoryStorageEngine(false);
            ObjectStore = ObjectStore.New(StorageEngine);
        }

        private InMemoryStorageEngine StorageEngine { get; set; }
        private ObjectStore ObjectStore { get; set; }

        [TestMethod]
        public void ReferenceWithValueIsSerializedAndDeserializedCorrectly()
        {
            var reference = new Reference(10);

            ObjectStore.Entangle(reference);
            ObjectStore.Persist();

            ObjectStore = ObjectStore.Load(StorageEngine, false);
            reference = ObjectStore.Resolve<Reference>();
            reference.Value.GetValue.ShouldBe(10);
        }

        [TestMethod]
        public void ReferencePersistableReferenceIsSerializedAndDeserializedCorrectly()
        {
            var reference = new Reference(new Simple() {Value = "1234"});

            ObjectStore.Entangle(reference);
            ObjectStore.Persist();

            ObjectStore = ObjectStore.Load(StorageEngine, false);
            reference = ObjectStore.Resolve<Reference>();
            Simple s = null;

            reference.DoWhenResolved<Simple>(simple => s = simple);
            s.Value.ShouldBe("1234");
        }

        private class Simple : IPersistable
        {
            public string Value { get; set; }


            public void Serialize(StateMap sd, SerializationHelper helper)
                => sd.Set(nameof(Value), Value);

            private static Simple Deserialize(IReadOnlyDictionary<string, object> sd)  
                => new Simple() {Value = sd.Get<string>(nameof(Value))};
        }

        [TestMethod]
        public void UseReferenceAndLoadAgain()
        {
            var p1 = new Person {Name = "Ole"};
            var p2 = new Person {Name = "Peter"};

            p1.Other = p2;
            p2.Other = p1;

            ObjectStore.Entangle(p1);
            ObjectStore.Entangle(p2);
            ObjectStore.Persist();

            var loadedPerson = Load();
            loadedPerson.Name.ShouldBe("Ole");
            loadedPerson.Other.Name.ShouldBe("Peter");

            var count = StorageEngine
                .SerializerTypes
                .Count(t => typeof(ReferenceSerializer) == t);
            count.ShouldBe(4);

            Persist();

            //Number of references should not change when references are pointing to the same instances

            loadedPerson = Load();
            loadedPerson.Name.ShouldBe("Ole");
            loadedPerson.Other.Name.ShouldBe("Peter");
            
            count = StorageEngine
                .SerializerTypes
                .Count(t => typeof(ReferenceSerializer) == t);
            count.ShouldBe(4);

            //Number of references should increase by one when person object is referencing itself
            loadedPerson.Other = loadedPerson;
            Persist();

            loadedPerson = Load();
            loadedPerson.Name.ShouldBe("Ole");
            loadedPerson.Other.Name.ShouldBe("Ole");
            
            count = StorageEngine
                .SerializerTypes
                .Count(t => typeof(ReferenceSerializer) == t);
            count.ShouldBe(5);
        }

        private Person Load()
        {
            ObjectStore = ObjectStore.Load(StorageEngine, false);
            return ObjectStore.Roots.All().OfType<Person>().Single(p => p.Name == "Ole");
        }

        private void Persist() => ObjectStore.Persist();

        private class Person : IPersistable
        {
            public string Name { get; set; }
            public Person Other { get; set; }

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(Name), Name);
                sd.Set(nameof(Other), helper.GetReference(Other));
            }

            private static Person Deserialize(IReadOnlyDictionary<string, object> sd)
            {
                var person = new Person { Name = (string)sd[nameof(Name)] };
                sd[nameof(Other)].CastTo<Reference>().DoWhenResolved<Person>(p => person.Other = p);
                return person;
            }
        }
    }
}
