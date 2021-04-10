using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers;

namespace Cleipnir.ObjectDB.Version2.PersistentDataStructures
{
    public class CSet<T> : IPersistable2, IEnumerable<T>
    {
        private Dictionary<T, ChangeTracker<T>.Node> _nodes;
        private readonly ChangeTracker<T> _changeTracker;
        private bool _dictionaryCreated;

        public CSet()
        {
            _nodes = new Dictionary<T, ChangeTracker<T>.Node>();
            _changeTracker = new ChangeTracker<T>();
            _dictionaryCreated = true;
        }

        private CSet(ChangeTracker<T> changeTracker)
        {
            _changeTracker = changeTracker;
            _dictionaryCreated = false;
        }

        public int Count 
        {
            get
            {
                if (!_dictionaryCreated)
                    CreateDictionary();
                
                return _nodes.Count;
            }
        } 

        public bool Add(T toAdd)
        {
            if (!_dictionaryCreated)
                CreateDictionary();
            
            if (_nodes.ContainsKey(toAdd)) return false;

            _nodes[toAdd] = _changeTracker.Append(toAdd);
            
            return true;
        }

        public void Remove(T toRemove)
        {
            if (!_dictionaryCreated)
                CreateDictionary();
            
            if (!_nodes.ContainsKey(toRemove)) return;

            _nodes[toRemove].Remove();
            _nodes.Remove(toRemove);
        }

        public bool Contains(T t)
        {
            if (!_dictionaryCreated)
                CreateDictionary();
            
            return _nodes.ContainsKey(t);
        } 

        private void CreateDictionary()
        {
            _nodes = _changeTracker.GetAllNodes().ToDictionary(n => n.Value, n => n);
            _dictionaryCreated = true;
        }
        
        public void Serialize(Map2 m) => _changeTracker.Serialize(m);

        private static CSet<T> Deserialize(RMap rm) => new CSet<T>(ChangeTracker<T>.Deserialize(rm));

        public IEnumerator<T> GetEnumerator()
        {
            if (!_dictionaryCreated)
                CreateDictionary();
            
            return _nodes.Values.Select(n => n.Value).GetEnumerator();  
        } 

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
