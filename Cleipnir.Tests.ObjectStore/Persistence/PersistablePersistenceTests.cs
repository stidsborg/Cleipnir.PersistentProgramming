using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.StorageEngine.InMemory;
using Cleipnir.Tests.ObjectStore.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ObjectStore.Persistence
{
    [TestClass]
    public class PersistablePersistenceTests
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

            var deserializedState = Deserializer2.Load(storageEngine, new Ephemerals(), serializerFactories);

            var deserializedPerson = deserializedState.Roots.Find<Person>();
            
            deserializedPerson.Name.ShouldBe("Peter");
            deserializedPerson.Relationship.ShouldBeNull();
        }
    }
}