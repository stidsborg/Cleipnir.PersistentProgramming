using System.Threading;
using Cleipnir.ObjectDB.Persistency;

namespace Cleipnir.ExecutionEngine.Api
{
    public static class Roots
    {
        internal static ThreadLocal<IRoots> Instance { get; } = new ThreadLocal<IRoots>();
        public static void Entangle(object persistable) => Instance.Value.Entangle(persistable);
        public static void Untangle(object persistable) => Instance.Value.Untangle(persistable);

        public static void EntangleAnonymously(object instance) => Instance.Value.EntangleAnonymously(instance);
        public static void UntangleAnonymously(object instance) => Instance.Value.UntangleAnonymously(instance);

        public static T Resolve<T>() => Instance.Value.Resolve<T>();
    }
}
