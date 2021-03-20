using System;
using System.Collections;
using System.Collections.Generic;

namespace Cleipnir.ObjectDB.Persistency.Version2
{
    public class Ephemerals : IEnumerable<object>
    {
        private readonly IReadOnlySet<object> _instances;
        public T Resolve<T>() => throw new NotImplementedException();

        public IEnumerable<object> All() => _instances;

        public IEnumerator<object> GetEnumerator() => _instances.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}