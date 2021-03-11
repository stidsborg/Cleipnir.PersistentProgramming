using System;
using System.Collections.Generic;
using System.Linq;

namespace Cleipnir.ObjectDB.Persistency.Version2
{
    public class Map2
    {
        private readonly MapAndSerializers _mapAndSerializers;

        private readonly Dictionary<string, Entry> _entries = new();

        private Dictionary<string, SerializerOrValue> _changedEntries = new();
        private readonly Dictionary<string, ISerializer2> _serializables = new();
        private readonly HashSet<string> _removedKeys = new();

        public Map2(MapAndSerializers mapAndSerializers) => _mapAndSerializers = mapAndSerializers;

        public Map2(MapAndSerializers mapAndSerializers, IEnumerable<SerializerOrValue> existingEntries)
        {
            _mapAndSerializers = mapAndSerializers;

            foreach (var existingEntry in existingEntries)
            {
                if (existingEntry.HoldsSerializer)
                {
                    _serializables[existingEntry.Key] = existingEntry.Serializer;
                    _entries[existingEntry.Key] = new Entry(existingEntry.Serializer.Instance);
                } else 
                    _entries[existingEntry.Key] = new Entry(existingEntry.Value);
            }
        }

        public object this[string key]
        {
            set => Set(key, value);
        }
        
        internal IEnumerable<SerializerOrValue> PullChangedEntries()
        {
            var changedEntryValues = _changedEntries.Values.ToList();
            _changedEntries.Clear();
            
            return changedEntryValues;
        }

        internal IEnumerable<string> PullRemovedKeys()
        {
            if (_removedKeys.Count == 0)
                return Enumerable.Empty<string>();
            
            var toReturn = _removedKeys.ToList();
            _removedKeys.Clear();
            
            return toReturn;
        }

        internal IEnumerable<ISerializer2> GetReferencedSerializers() => _serializables.Values;
        
        public void Set(string key, object value)
        {
            _removedKeys.Remove(key);

            if (!_entries.ContainsKey(key))
                _entries[key] = new Entry(value);
            else
            {
                var changed = _entries[key].Set(value);
                if (!changed) return;
            }

            if (value == null || IsPrimitive(value))
            {
                _changedEntries[key] = SerializerOrValue.CreateValue(key, value);
                _serializables.Remove(key);
            }
            else
            {
                var serializer = _mapAndSerializers.GetOrCreateSerializerFor(value);
                _serializables[key] = serializer;
                _changedEntries[key] = SerializerOrValue.CreateSerializer(key, serializer);
            }
        }

        public void Remove(string key)
        {
            if (!_entries.ContainsKey(key)) return;
            
            _serializables.Remove(key);
            _changedEntries.Remove(key);
            _removedKeys.Add(key);
        }

        private static bool IsPrimitive(object o)
            => o == null || o.GetType().IsPrimitive || o is DateTime || o is string || o is Guid;

        private class Entry
        {
            public object Value { get; private set; }
            
            public Entry(object value) => Value = value;

            /// <summary>
            /// Sets the entry's
            /// </summary>
            /// <param name="newValue">The entry's new value</param>
            /// <returns>Returns true if the entry's value changed otherwise false</returns>
            public bool Set(object newValue)
            {
                if (Value == null && newValue == null)
                    return false;

                if (Value != null && newValue != null && Value.Equals(newValue))
                    return false;

                Value = newValue;
                return true;
            }
        }
    }
    
    public class SerializerOrValue
    {
        public string Key { get; }

        public bool HoldsSerializer => Serializer != null;
        public ISerializer2 Serializer { get; }
        public object Value { get; }

        private SerializerOrValue(string key, ISerializer2 serializer)
        {
            Key = key;
            Serializer = serializer;
            Value = null;
        }

        private SerializerOrValue(string key, object value)
        {
            Key = key;
            Serializer = null;
            Value = value;
        }

        public static SerializerOrValue CreateValue(string key, object value) => new SerializerOrValue(key, value);
        public static SerializerOrValue CreateSerializer(string key, ISerializer2 serializer) => new SerializerOrValue(key, serializer);
    }
}