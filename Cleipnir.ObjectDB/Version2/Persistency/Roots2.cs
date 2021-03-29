using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers.Persistable;

namespace Cleipnir.ObjectDB.Version2.Persistency
{
    internal class Roots2 : IRoots2, IPersistable2
    {
        public const long ObjectId = 0;
        
        private RootSet _roots = new();
        private RootSet _anonymousRoots = new();

        public void Entangle(object persistable) => _roots.Add(persistable);
        public void Untangle(object persistable) => _roots.Remove(persistable);

        public void EntangleAnonymously(object instance) => _anonymousRoots.Add(instance);
        public void UntangleAnonymously(object instance) => _anonymousRoots.Remove(instance);

        public T Find<T>() => (T) _roots.Single(i => i is T);

        public IEnumerable<object> All() => _roots.ToList();
        
        public void Serialize(Map2 m)
        {
            _roots.SerializeInto(m, "");
            _anonymousRoots.SerializeInto(m, "anonymous");
        }

        private static Roots2 Deserialize(RMap rm)
            => new Roots2
            {
                _roots = RootSet.DeserializeFrom(rm, ""),
                _anonymousRoots = RootSet.DeserializeFrom(rm, "anonymous")
            };

        public IEnumerator<object> GetEnumerator() => _roots.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
