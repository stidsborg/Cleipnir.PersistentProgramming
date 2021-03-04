using System;
using System.Threading;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;

namespace Cleipnir.ExecutionEngine.Api
{
    public static class Scheduler
    {
        internal static ThreadLocal<IScheduler> ThreadLocalScheduler { get; } = new ThreadLocal<IScheduler>();

        public static void Schedule(Action toExecute, bool persistable = true)
        {
            ThreadLocalScheduler.Value.Schedule(toExecute, persistable);
        }

        public static CAwaitable Yield(bool persistable = true)
        {
            var awaitable = new CAwaitable();
            ThreadLocalScheduler.Value.Schedule(awaitable.SignalCompletion, persistable);
            return awaitable;
        }
    }
}
