using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers;

namespace Cleipnir.ObjectDB.Version2.PersistentDataStructures
{
    public class CDictionary<TKey, TValue> : IPersistable2, IEnumerable<CDictionary<TKey, TValue>.KeyAndValue>
    {
        private Dictionary<TKey, ChangeTracker<KeyAndValue>.Node> _nodesDictionary = new();
        private readonly ChangeTracker<KeyAndValue> _changeTracker = new();
        private bool _dictionaryCreated = true;

        public CDictionary() {}

        private CDictionary(ChangeTracker<KeyAndValue> changeTracker)
        {
            _changeTracker = changeTracker;
            _dictionaryCreated = false;
        }
        
        public TValue this[TKey key]
        {
            get => Get(key);
            set => Set(key, value);
        }

        public int Count
        {
            get
            {
                if (!_dictionaryCreated)
                    CreateDictionary();
                
                return _nodesDictionary.Count;                
            }
        }

        public bool ContainsKey(TKey key)
        {
            if (!_dictionaryCreated)
                CreateDictionary(); 
            
            return _nodesDictionary.ContainsKey(key);
        }
        
        public IEnumerable<TValue> Values => KeyAndValues().Select(kv => kv.Value);
        public IEnumerable<TKey> Keys => KeyAndValues().Select(kv => kv.Key);

        public TValue Get(TKey key)
        {
            if (!_dictionaryCreated)
                CreateDictionary();
            
            return _nodesDictionary[key].Value.Value;
        } 
        
        public void Set(TKey key, TValue value)
        {
            if (!_dictionaryCreated)
                CreateDictionary();

            if (!_nodesDictionary.ContainsKey(key))
                _nodesDictionary[key] = _changeTracker.Append(
                    new KeyAndValue {Key = key}
                );
                    
            _nodesDictionary[key].Value.Value = value;
        }

        public void Remove(TKey key)
        {
            if (!_dictionaryCreated)
                CreateDictionary();
            
            if (!_nodesDictionary.ContainsKey(key)) return;

            _nodesDictionary[key].Remove();
            _nodesDictionary.Remove(key);
        }

        private void CreateDictionary()
        {
            _nodesDictionary = _changeTracker
                .GetAllNodes()
                .ToDictionary(
                    n => n.Value.Key,
                    n => n
                );
            
            _dictionaryCreated = true;
        }
        
        public void Serialize(Map2 m) => _changeTracker.Serialize(m);

        private static CDictionary<TKey, TValue> Deserialize(RMap rm)
            => new CDictionary<TKey, TValue>(ChangeTracker<KeyAndValue>.Deserialize(rm));
        
        public class KeyAndValue : IPersistable2
        {
            public TKey Key { get; internal set; }
            public TValue Value { get; internal set; }

            public void Serialize(Map2 m)
            {
                m[nameof(Key)] = Key;
                m[nameof(Value)] = Value;
            }

            private static KeyAndValue Deserialize(RMap rm) 
                => new KeyAndValue()
                {
                    Key = rm.Get<TKey>(nameof(Key)),
                    Value = rm.Get<TValue>(nameof(Value))
                };
        }

        private IEnumerable<KeyAndValue> KeyAndValues()
        {
            if (!_dictionaryCreated)
                CreateDictionary();

            return _nodesDictionary
                .Values
                .Select(node => node.Value);
        }

        public IEnumerator<KeyAndValue> GetEnumerator() => KeyAndValues().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
