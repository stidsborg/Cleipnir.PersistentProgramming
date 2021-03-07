using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.Persistency.Persistency;

namespace Cleipnir.ObjectDB.Persistency
{
    internal class Roots : IRoots, IPersistable
    {
        private CSet<object> _roots = new CSet<object>();
        private CSet<object> _anonymousRoots = new CSet<object>();
        private bool _serialized;
        public IEnumerable<object> All() => _roots;

        public void Entangle(object persistable) => _roots.Add(persistable);
        public void Untangle(object persistable) => _roots.Remove(persistable);

        public void EntangleAnonymously(object instance) => _anonymousRoots.Add(instance);
        public void UntangleAnonymously(object instance) => _anonymousRoots.Remove(instance);

        public T Resolve<T>() => (T) _roots.Single(i => i is T);

        public IEnumerable<T> ResolveAll<T>() => _roots.Where(i => i is T).Cast<T>().ToList(); 
        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_serialized) return; 
            _serialized = true;

            sd.Set(nameof(_roots), helper.GetReference(_roots));
            sd.Set(nameof(_anonymousRoots), helper.GetReference(_anonymousRoots));
        }
        
        private static Roots Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            var roots = new Roots {_serialized = true};
            
            var reference = (Reference) sd[nameof(_roots)];
            reference.DoWhenResolved<CSet<object>>(set => roots._roots = set);

            reference = (Reference) sd[nameof(_anonymousRoots)];
            reference.DoWhenResolved<CSet<object>>(set => roots._anonymousRoots = set);

            return roots;
        }
    }
}
