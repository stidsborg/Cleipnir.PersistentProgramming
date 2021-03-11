using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Cleipnir.ObjectDB.Persistency.Version2
{
    public class MapAndSerializers
    {
        private readonly Dictionary<long, ISerializer2> _serializers = new();
        private readonly Dictionary<long, Map2> _maps = new();

        private readonly Dictionary<object, long> _objectToId = new(new ObjectReferenceEqualityComparer<object>());

        private readonly SerializerFactories _factories;

        private long _nextObjectId = 0;
        
        public MapAndSerializers(SerializerFactories factories) => _factories = factories;

        public bool IsSerializable(object o) => _factories.IsSerializable(o);

        public ISerializer2 this[long id] => _serializers[id];
        
        public ISerializer2 GetOrCreateSerializerFor(object instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            
            if (!_objectToId.ContainsKey(instance))
                _objectToId[instance] = _nextObjectId++;
                
            var objectId = _objectToId[instance];
            if (_serializers.ContainsKey(objectId))
                return _serializers[objectId];

            var factory = _factories.Find(instance);

            if (factory == null)
                throw new ArgumentException($"Serialization of {instance.GetType()} not supported. Did you forget to add custom serializer?");

            var serializer = factory.CreateSerializer(instance);
            _serializers[objectId] = serializer;
            _maps[objectId] = new Map2(this);
            
            return serializer;
        }
        
        public void Add(long objectId, ISerializer2 serializer, Map2 m)
        {
            _objectToId[serializer.Instance] = objectId;
            _serializers[objectId] = serializer;
            _maps[objectId] = m;
            _nextObjectId = Math.Max(_nextObjectId, objectId + 1);
        }

        private class ObjectReferenceEqualityComparer<T> : EqualityComparer<T> where T : class
        {
            public override bool Equals(T x, T y) => ReferenceEquals(x, y);

            public override int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
        }
    }
}