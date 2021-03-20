using System;
using Cleipnir.ObjectDB.Persistency.Version2;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ObjectStore
{
    [TestClass]
    public class PersistenceTests
    {
        [TestMethod]
        public void SerializeAndDeserializeStateWorksInSimpleCase()
        {
            var storageEngine = new InMemoryStorageEngine();
            var roots = new Roots2();
            var serializerFactories = new SerializerFactories(new[] {new PersistableSerializer2()});
            var mps = new MapAndSerializers(serializerFactories);

            mps.GetOrCreateSerializerFor(roots);
            
            roots.Entangle(new Person {Name = "Peter"});

            var persister = new Persister2(storageEngine, mps);
            persister.DetectAndPersistChanges();

            var storedState = storageEngine.Load();

            var d = new D(storedState, new Ephemerals(), serializerFactories);
            var deserializedState = d.Deserialize();

            var deserializedPerson = deserializedState.Roots.Resolve<Person>();
            
            deserializedPerson.Name.ShouldBe("Peter");
            deserializedPerson.Other.ShouldBeNull();
        }
    }
}