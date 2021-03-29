using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.Helpers;

namespace Cleipnir.ObjectDB.Version2.Persistency
{
    public class Ephemerals : IEnumerable<object>
    {
        private readonly IReadOnlySet<object> _instances;

        public Ephemerals() : this(Enumerable.Empty<object>()) {}
        public Ephemerals(IEnumerable<object> instances) 
            => _instances = new HashSet<object>(instances);

        public T Resolve<T>() => _instances.Single(i => i is T).CastTo<T>();

        public IEnumerator<object> GetEnumerator() => _instances.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}