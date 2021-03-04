using System.Collections.Generic;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ObjectStoreTests
{
    [TestClass]
    public class InMemoryStoreTests
    {
        [TestMethod]
        public void SerializeAndDeserializePersonWithParent()
        {
            var storageEngine = new InMemoryStorageEngine();
            var os = ObjectStore.New(storageEngine);
            
            var parent = new Person() {Name = "Oldy", Parent = null};
            var child = new Person() {Name = "Childy", Parent = parent};
            
            os.Entangle(child);
            os.Persist();

            os = ObjectStore.Load(storageEngine);
            var pChild = os.Resolve<Person>();
            var pParent = pChild.Parent;
            
            pChild.Name.ShouldBe("Childy");
            pParent.Name.ShouldBe("Oldy");
            pParent.Parent.ShouldBeNull();

            pChild.Parent = null;
            os.Persist();
            
            os = ObjectStore.Load(storageEngine);
            pChild = os.Resolve<Person>();
            pChild.Parent.ShouldBeNull();
        } 
        
        class Person : IPersistable
        {
            public string Name { get; set; }
            public Person Parent { get; set; }


            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd[nameof(Name)] = Name;
                sd[nameof(Parent)] = Parent;
            }

            private static Person Deserialize(IReadOnlyDictionary<string, object> sd)
            {
                return new Person()
                {
                    Name = sd.Get<string>(nameof(Name)),
                    Parent = sd.Get<Person>(nameof(Parent))
                };
            }
        }
    }


}