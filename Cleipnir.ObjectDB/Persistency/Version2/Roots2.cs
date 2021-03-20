using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Cleipnir.ObjectDB.Persistency.Version2
{
    internal class Roots2 : IRoots, IPersistable2, IEnumerable<object>
    {
        public const long ObjectId = 0;
        
        private ListSet _roots = new();
        private ListSet _anonymousRoots = new();

        public void Entangle(object persistable) => _roots.Add(persistable);
        public void Untangle(object persistable) => _roots.Remove(persistable);

        public void EntangleAnonymously(object instance) => _anonymousRoots.Add(instance);
        public void UntangleAnonymously(object instance) => _anonymousRoots.Remove(instance);

        public T Resolve<T>() => (T) _roots.Single(i => i is T);

        public IEnumerable<object> All() => _roots.ToList();

        public IEnumerable<T> ResolveAll<T>() => _roots.Where(i => i is T).Cast<T>().ToList(); 
        
        public void Serialize(Map2 m)
        {
            _roots.SerializeInto(m, "");
            _anonymousRoots.SerializeInto(m, "anonymous");
        }

        private static Roots2 Deserialize(RMap rm)
            => new Roots2
            {
                _roots = ListSet.DeserializeFrom(rm, ""),
                _anonymousRoots = ListSet.DeserializeFrom(rm, "anonymous")
            };

        public IEnumerator<object> GetEnumerator() => _roots.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
