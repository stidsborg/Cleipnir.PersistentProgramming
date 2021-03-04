using System;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Api;

namespace Cleipnir.Tests.Helpers
{
    internal static class Extensions
    {
        public static T CastTo<T>(this object o) => (T) o;

        public static void BusyWait(Func<bool> @while)
        {
            while (@while())
                Thread.Sleep(1);
        }

        public static Task<T> Resolve<T>(this Engine scheduler)
            => scheduler.Schedule(Roots.Resolve<T>);

        public static void Do<T>(this Engine scheduler, Action<T> action)
            => scheduler.Schedule(() => action(Roots.Resolve<T>()));
    }
}
