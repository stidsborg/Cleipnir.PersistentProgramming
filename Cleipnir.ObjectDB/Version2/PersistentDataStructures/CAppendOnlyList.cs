using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers;

namespace Cleipnir.ObjectDB.Version2.PersistentDataStructures
{
    public class CAppendOnlyList<T> : IPersistable2, IEnumerable<T>
    {
        private readonly ChangeTracker<T> _changeTracker;

        public CAppendOnlyList() => _changeTracker = new ChangeTracker<T>();
        private CAppendOnlyList(ChangeTracker<T> ct) => _changeTracker = ct; 
        
        public void Add(T toAdd) => _changeTracker.Append(toAdd);

        public void Serialize(Map2 m) => _changeTracker.Serialize(m);

        private static CAppendOnlyList<T> Deserialize(RMap rm)
            => new CAppendOnlyList<T>(ChangeTracker<T>.Deserialize(rm));

        public IEnumerator<T> GetEnumerator() => _changeTracker.GetAllNodes().Select(n => n.Value).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
