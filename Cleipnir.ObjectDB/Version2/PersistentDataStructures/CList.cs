using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers;

namespace Cleipnir.ObjectDB.Version2.PersistentDataStructures
{
    public class CList<T> : IPersistable2, IEnumerable<T>
    {
        private readonly List<ChangeTracker<T>.Node> _inner;
        private readonly ChangeTracker<T> _changeTracker;

        public T this[int index]
        {
            get => _inner[index].Value;
            set => Set(index, value);
        }

        public CList()
        {
            _inner = new List<ChangeTracker<T>.Node>();
            _changeTracker = new ChangeTracker<T>();
        }

        private CList(ChangeTracker<T> changeTracker)
        {
            _changeTracker = changeTracker;
            _inner = new List<ChangeTracker<T>.Node>(changeTracker.GetAllNodes());
        }
        
        public int Count => _inner.Count;

        public void Add(T elem) => _inner.Add(_changeTracker.Append(elem));

        public void Set(int index, T value) => _inner[index].Value = value;

        public void Remove(int index)
        {
            _inner[index].Remove();
            _inner.RemoveAt(index);
        }
        
        public void Serialize(Map2 m) => _changeTracker.Serialize(m);

        private static CList<T> Deserialize(RMap rm) => new CList<T>(ChangeTracker<T>.Deserialize(rm));
        
        public IEnumerator<T> GetEnumerator() => _inner.Select(n => n.Value).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
