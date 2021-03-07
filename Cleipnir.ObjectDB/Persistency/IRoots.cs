using System.Collections.Generic;

namespace Cleipnir.ObjectDB.Persistency
{
    public interface IRoots
    {
        void Entangle(object o);
        void Untangle(object o);

        void EntangleAnonymously(object o);
        void UntangleAnonymously(object o);

        T Resolve<T>();
        IEnumerable<object> All();
    }
}