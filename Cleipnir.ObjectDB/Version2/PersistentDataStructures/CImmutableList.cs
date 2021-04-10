using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers;

namespace Cleipnir.ObjectDB.Version2.PersistentDataStructures
{
    public class CImmutableList<T> : IPersistable2, IEnumerable<T>
    {
        private readonly T[] _inner;
        private bool _serialized;

        public CImmutableList(T[] inner) => _inner = inner;

        public T this[int index] => _inner[index];
        public int Count => _inner.Length;

        public void Serialize(Map2 m)
        {
            if (_serialized) return; _serialized = true;

            m.Set("Length", _inner.Length);
            for (var i = 0; i < _inner.Length; i++)
                m.Set(i.ToString(), _inner[i]);
        }

        private static CImmutableList<T> Deserialize(RMap rm)
        {
            var length = rm.Get<int>("Length");
            var inner = new T[length];

            for (var i = 0; i < length; i++)
            {
                var j = i;
                rm.WhenResolved<T>(
                    j.ToString(), 
                    t => inner[j] = t 
                );
            }

            return new CImmutableList<T>(inner) { _serialized = true };
        }

        public IEnumerator<T> GetEnumerator() => _inner.Select(_ => _).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}