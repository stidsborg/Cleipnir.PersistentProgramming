using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Cleipnir.ObjectDB.Persistency.Version2
{
    public class RootSet : IEnumerable<object>
    {
        private readonly List<object> _entries;

        public RootSet(int capacity = 0) => _entries = new List<object>(capacity); 
        
        public void Add(object root)
        {
            if (!_entries.Contains(root))
                _entries.Add(root);
        }

        public void Remove(object root)
        {
            var entryIndex = _entries.IndexOf(root);
            if (entryIndex == -1)
                return;

            var lastEntry = _entries[^1];
            _entries[entryIndex] = lastEntry;
            _entries.RemoveAt(_entries.Count - 1);
        }

        public void SerializeInto(Map2 m, string keyPrefix = "")
        {
            for (var i = 0; i < _entries.Count; i++)
                m[keyPrefix + i] = _entries[i];

            m[$"{keyPrefix}Count"] = _entries.Count;
        }

        public static RootSet DeserializeFrom(RMap rm, string keyPrefix = "")
        {
            var listSet = new RootSet();
            if (!rm.ContainsKey($"{keyPrefix}Count")) return listSet;
            var elms = new object[rm.Get<int>($"{keyPrefix}Count")];

            for (var i = 0; rm.ContainsKey(keyPrefix + i); i++)
            {
                var j = i;
                rm.WhenResolved<object>(
                    keyPrefix + j,
                    o =>
                    {
                        elms[j] = o;
                        if (elms.All(e => e != null)) listSet._entries.AddRange(elms); 
                    });
            }

            return listSet;
        }

        public IEnumerator<object> GetEnumerator() => _entries.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}