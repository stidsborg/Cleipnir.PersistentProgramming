using System;
using System.Threading;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;

namespace Cleipnir.ExecutionEngine.Api
{
    public static class Sync
    {
        internal static ThreadLocal<SynchronizationQueue> SynchronizationQueue { get; } = new ThreadLocal<SynchronizationQueue>();
        public static CAwaitable Next(bool persistable = true) => SynchronizationQueue.Value.Sync(persistable);

        public static void AfterNext(Action callback, bool persistable = true) => SynchronizationQueue.Value.Sync(callback, persistable);
    }
}