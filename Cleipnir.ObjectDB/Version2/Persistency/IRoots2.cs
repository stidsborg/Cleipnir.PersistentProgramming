using System.Collections.Generic;

namespace Cleipnir.ObjectDB.Version2.Persistency
{
    public interface IRoots2 : IEnumerable<object>
    {
        void Entangle(object persistable);
        void Untangle(object o);

        T Find<T>();
        
        void EntangleAnonymously(object o);
        void UntangleAnonymously(object o);
    }
}