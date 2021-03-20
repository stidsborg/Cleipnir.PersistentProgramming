using System;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Persistency.Version2;
using Cleipnir.StorageEngine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ObjectStore
{
    [TestClass]
    public class HardCodedDeserializationTests
    {
        [TestMethod]
        public void DeserializationOne()
        {
            //arrange
            var testStorage = new TestStorageEngine();
            var eps = new Ephemerals();
            var s = new PersistableSerializer2();
            var factories = new SerializerFactories(new List<ISerializerFactory>() {s});

            //act
            var state = D.Load(testStorage, eps, factories);

            //assert
            state.ShouldNotBeNull();

            var peter = (Person) state.Roots.Single(o => o is Person p && p.Name == "Peter");
            var ole = (Person) state.Roots.Single(o => o is Person p && p.Name == "Ole");
            peter.Other.ShouldBe(ole);
            ole.Other.ShouldBe(peter);

            (state.MapAndSerializers[0].Serializer.Instance is Roots2).ShouldBeTrue();
            (state.MapAndSerializers[1].Serializer.Instance is Person).ShouldBeTrue();
            (state.MapAndSerializers[2].Serializer.Instance is Person).ShouldBeTrue();
        }

        private class TestStorageEngine : IStorageEngine
        {
            public StoredState Load()
            {
                var serializers = new Dictionary<long, Type>();
                var entries = new Dictionary<long, IEnumerable<StorageEntry>>();
                entries[0] = Enumerable
                    .Empty<StorageEntry>()
                    .Append(new StorageEntry(0, "0", 1L))
                    .Append(new StorageEntry(0, "1", 2L))
                    .Append(new StorageEntry(0, "Count", (object) 2))
                    .Append(new StorageEntry(0, "¡Type", typeof(Roots2).SimpleQualifiedName()));
                entries[1] = Enumerable
                    .Empty<StorageEntry>()
                    .Append(new StorageEntry(1, "Name", "Peter"))
                    .Append(new StorageEntry(1, "Other", 2L))
                    .Append(new StorageEntry(1, "¡Type", typeof(Person).SimpleQualifiedName()));
                entries[2] = Enumerable
                    .Empty<StorageEntry>()
                    .Append(new StorageEntry(2, "Name", "Ole"))
                    .Append(new StorageEntry(2, "Other", 1L))
                    .Append(new StorageEntry(2, "¡Type", typeof(Person).SimpleQualifiedName()));

                serializers[0] = typeof(PersistableSerializer2);
                serializers[1] = typeof(PersistableSerializer2);
                serializers[2] = typeof(PersistableSerializer2);

                return new StoredState(serializers, entries);
            }
            
            public void Persist(DetectedChanges detectedChanges) { }
            public void Dispose() { }
        }
    }
}