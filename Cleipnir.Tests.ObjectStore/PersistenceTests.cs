using Cleipnir.ObjectDB.Persistency.Version2;
using Cleipnir.ObjectDB.Persistency.Version2.Serializers;
using Cleipnir.ObjectDB.Persistency.Version2.Serializers.Persistable;
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
            var serializerFactories = SerializerFactories.Default;
            var mps = new MapAndSerializers(serializerFactories);

            mps.GetOrCreateSerializerFor(roots);
            
            roots.Entangle(new Person {Name = "Peter"});

            var persister = new Persister2(storageEngine, mps);
            persister.DetectAndPersistChanges();

            var deserializedState = D.Load(storageEngine, new Ephemerals(), serializerFactories);

            var deserializedPerson = deserializedState.Roots.Find<Person>();
            
            deserializedPerson.Name.ShouldBe("Peter");
            deserializedPerson.Other.ShouldBeNull();
        }
    }
}