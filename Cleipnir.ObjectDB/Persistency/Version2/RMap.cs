using System;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Persistency.Version2
{
    public class RMap
    {
        private readonly D _deserializer;
        private readonly Dictionary<string, StorageEntry> _storageEntries;

        private readonly Dictionary<string, SerializerOrValue> _resolvedValues = new();
        
        internal RMap(D deserializer, IEnumerable<StorageEntry> storageEntries)
        {
            _deserializer = deserializer;
            _storageEntries = storageEntries.ToDictionary(e => e.Key, e => e);
        }
        
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
                var serializer = _deserializer.Deserialize(entry.Reference.Value);
                _resolvedValues[name] = SerializerOrValue.CreateSerializer(name, serializer);
                return (T) serializer.Instance;
            }
        }

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
                _deserializer.RegisterCallbackWhenResolved(entry.Reference.Value, s =>
                {
                    _resolvedValues[name] = SerializerOrValue.CreateSerializer(name, s);
                    setter((T) s.Instance);
                });
            }
        }

        internal IEnumerable<SerializerOrValue> GetDeserializedValues()
            => _resolvedValues.Values;
    }
}