using System.Linq;
using Cleipnir.Helpers;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ObjectStore.Deserialization
{
    [TestClass]
    public class CircularityTests
    {
        [TestMethod]
        public void SerializeAndDeserializeStateWorksDetectsCircularDependencyOnDeserialization()
        {
            var storageEngine = new InMemoryStorageEngine();
            var roots = new Roots2();
            var serializerFactories = SerializerFactories.Default;
            var mps = new MapAndSerializers(serializerFactories);

            mps.GetOrCreateSerializerFor(roots);

            var p1 = new Person {Name = "Peter"};
            var p2 = new Person {Name = "Ole"};

            p1.Sibling = p2;
            p2.Sibling = p1;
            
            roots.Entangle(p1);

            var persister = new Persister2(storageEngine, mps);
            persister.DetectAndPersistChanges();

            try
            {
                Deserializer2.Load(storageEngine, new Ephemerals(), serializerFactories);
            } 
            catch (CircularDependencyException2 e)
            {
                e.CircularPath.Count().ShouldBe(3);
            }
        }
        
        [TestMethod]
        public void SerializeAndDeserializeStateWorksDetectsNonCircularDependencyOnDeserialization()
        {
            var storageEngine = new InMemoryStorageEngine();
            var roots = new Roots2();
            var serializerFactories = SerializerFactories.Default;
            var mps = new MapAndSerializers(serializerFactories);

            mps.GetOrCreateSerializerFor(roots);

            var p1 = new Person {Name = "Peter"};
            var p2 = new Person {Name = "Ole"};

            p1.Sibling = p2;

            roots.Entangle(p1);

            var persister = new Persister2(storageEngine, mps);
            persister.DetectAndPersistChanges();
            
            var (r, _) = Deserializer2.Load(storageEngine, new Ephemerals(), serializerFactories);
            var p = r.Find<Person>();
            p.Name.ShouldBe("Peter");
            p.Sibling.Name.ShouldBe("Ole");
        }

        private class Person : IPersistable2
        {
            public string Name { get; set; }
            public Person Sibling { get; set; }
            
            public void Serialize(Map2 m)
            {
                m[nameof(Name)] = Name;
                m[nameof(Sibling)] = Sibling;
            }

            private static Person Deserialize(RMap rm)
                => new Person
                {
                    Name = rm[nameof(Name)].CastTo<string>(),
                    Sibling = rm[nameof(Sibling)].CastTo<Person>()
                };
        }
    }
}