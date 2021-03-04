using System;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;

namespace Cleipnir.Rx
{
    public static class Extensions
    {
        public static IDisposable CallOnEvent<T>(this Stream<T> stream, Action<T> onNext)
            => new CallOnEventSubscription<T>(stream, onNext);

        public static CAwaitable<T> Next<T>(this Stream<T> stream)
            => new CallOnNextEventSubscription<T>(stream).Awaitable;
    }
}
