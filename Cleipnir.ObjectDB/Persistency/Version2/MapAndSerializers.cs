using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Persistency.Version2
{
    public class MapAndSerializers
    {
        private readonly Dictionary<long, MapAndSerializer> _mapAndSerializers = new();

        private readonly Dictionary<object, long> _objectToId = new(new ObjectReferenceEqualityComparer<object>());

        private readonly SerializerFactories _factories;

        private List<ObjectIdAndType> _newEntries = new();

        private long _nextObjectId = 0;
        
        public MapAndSerializers(SerializerFactories factories) => _factories = factories;

        public bool IsSerializable(object o) => _factories.IsSerializable(o);

        public MapAndSerializer this[long id] => _mapAndSerializers[id];

        public SerializerAndObjectId GetOrCreateSerializerFor(object instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            
            if (!_objectToId.ContainsKey(instance))
                _objectToId[instance] = _nextObjectId++;
                
            var objectId = _objectToId[instance];
            if (_mapAndSerializers.ContainsKey(objectId))
                return new SerializerAndObjectId(_mapAndSerializers[objectId].Serializer, objectId);

            var factory = _factories.Find(instance);

            if (factory == null)
                throw new ArgumentException($"Serialization of {instance.GetType()} not supported. Did you forget to add custom serializer?");

            var serializer = factory.CreateSerializer(instance);
            _mapAndSerializers[objectId] = new MapAndSerializer(new Map2(this), serializer);
            _newEntries.Add(new ObjectIdAndType(objectId, factory.GetType()));

            return new SerializerAndObjectId(serializer, objectId);
        }
        
        public void Add(long objectId, ISerializer2 serializer, Map2 map)
        {
            //todo test if object id already exist?
            _objectToId[serializer.Instance] = objectId;
            _mapAndSerializers[objectId] = new MapAndSerializer(map, serializer);
            _nextObjectId = Math.Max(_nextObjectId, objectId + 1);
        }

        public IEnumerable<ObjectIdAndType> PullNewSerializerFactoryTypes()
        {
            var newEntries = _newEntries;
            _newEntries = new List<ObjectIdAndType>();
            return newEntries;
        }

        private class ObjectReferenceEqualityComparer<T> : EqualityComparer<T> where T : class
        {
            public override bool Equals(T x, T y) => ReferenceEquals(x, y);

            public override int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }

    public record MapAndSerializer(Map2 Map, ISerializer2 Serializer);

    public record SerializerAndObjectId(ISerializer2 Serializer, long ObjectId);
}