using System.Linq;
using Cleipnir.ObjectDB.Persistency.Version2;
using Cleipnir.ObjectDB.Persistency.Version2.Serializers;
using Cleipnir.ObjectDB.Persistency.Version2.Serializers.Persistable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ObjectStore
{
    [TestClass]
    public class StateMapTests
    {
        [TestMethod]
        public void MapWithExistingSimpleValueDetectsChangesInValue()
        {
            var mps = new MapAndSerializers(SerializerFactories.Default);
            var existingValues = new[]
            {
                SerializerOrValue.CreateValue("Name", "Peter")
            };
            var map = new Map2(mps, existingValues);
            
            map.PullChangedEntries().ShouldBeEmpty();

            map["Name"] = "Peter";
            
            map.PullChangedEntries().ShouldBeEmpty();
            
            map["Name"] = "Ole";

            var changes = map.PullChangedEntries().ToArray();
            changes.Length.ShouldBe(1);
            changes[0].Key.ShouldBe("Name");
            changes[0].HoldsSerializer.ShouldBeFalse();
            changes[0].Value.ShouldBe("Ole");
        }
        
        [TestMethod]
        public void MapWithExistingReferencedValueDetectsChangesInValue()
        {
            var mps = new MapAndSerializers(SerializerFactories.Default);
            var peter = new Person() {Name = "Peter"};
            var peterSerializer = new PersistableSerializer2(peter, false);
            mps.Add(0, peterSerializer, new Map2(mps));
            
            var ole = new Person {Name = "Ole"};
            var existingValues = new[]
            {
                SerializerOrValue.CreateSerializer("Person", peterSerializer, 0)
            };
            var map = new Map2(mps, existingValues);
            
            map.PullChangedEntries().ShouldBeEmpty();

            map["Person"] = peter;
            
            map.PullChangedEntries().ShouldBeEmpty();
            
            map["Person"] = ole;

            var changes = map.PullChangedEntries().ToArray();
            changes.Length.ShouldBe(1);
            changes[0].Key.ShouldBe("Person");
            changes[0].HoldsSerializer.ShouldBeTrue();
            changes[0].SerializerAndObjectId.ObjectId.ShouldBe(1);
            changes[0].SerializerAndObjectId.Serializer.Instance.ShouldBe(ole);
        }
        
        private class Person : IPersistable2
        {
            public string Name { get; set; }
        
            public void Serialize(Map2 m) { }
        }
    }
}