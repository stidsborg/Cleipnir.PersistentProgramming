using System;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Version2.Persistency
{
    public class RMap
    {
        private readonly Deserializer2 _deserializer;
        private readonly Dictionary<string, StorageEntry> _storageEntries;

        private readonly Dictionary<string, SerializerOrValue> _resolvedValues = new();
        
        internal RMap(Deserializer2 deserializer, IEnumerable<StorageEntry> storageEntries)
        {
            _deserializer = deserializer;
            _storageEntries = storageEntries.ToDictionary(e => e.Key, e => e);
        }

        public object this[string key] => Get<object>(key);
        
        public T Get<T>(string name)
        {
            if (!_storageEntries.ContainsKey(name))
                throw new ArgumentException($"Entry {name} not found. Was it deleted or not serialized at all?");

            var entry = _storageEntries[name];
            if (!entry.Reference.HasValue)
            {
                var value = (T) entry.Value;
                _resolvedValues[name] = SerializerOrValue.CreateValue(name, value);
                return value;
            }
            else
            {
                var objectId = entry.Reference.Value;
                var serializer = _deserializer.Deserialize(objectId);
                _resolvedValues[name] = SerializerOrValue.CreateSerializer(name, serializer, objectId);
                return (T) serializer.Instance;
            }
        }

        public bool ContainsKey(string name) => _storageEntries.ContainsKey(name);

        public void WhenResolved<T>(string name, Action<T> setter)
        {
            if (!_storageEntries.ContainsKey(name))
                throw new ArgumentException($"Entry {name} not found. Was it deleted or not serialized at all?");

            var entry = _storageEntries[name];
            if (!entry.Reference.HasValue)
            {
                //todo better handle casting exceptions while deserializing

                var value = (T) entry.Value;
                _resolvedValues[name] = SerializerOrValue.CreateValue(name, value);
                setter(value);
            }
            else
            {
                //todo better handle casting exceptions while deserializing
                var objectId = entry.Reference.Value;
                _deserializer.RegisterCallbackWhenResolved(objectId, s =>
                {
                    _resolvedValues[name] = SerializerOrValue.CreateSerializer(name, s, objectId);
                    setter((T) s.Instance);
                });
            }
        }

        internal IEnumerable<SerializerOrValue> GetDeserializedValues()
            => _resolvedValues.Values;
    }
}