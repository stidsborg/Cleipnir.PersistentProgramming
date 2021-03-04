using System;
using Cleipnir.ExecutionEngine.DataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;

namespace Cleipnir.ExecutionEngine
{
    internal class SynchronizationQueue
    {
        private readonly CArray<CAction> _callbacks =  new CArray<CAction>();

        internal void MoveAllTo(ReadyToSchedules rts) => rts.MoveAllFrom(_callbacks);

        internal bool Empty => _callbacks.Empty;

        internal void Sync(Action callback, bool persistent)
            => _callbacks.Add(new CAction(callback, persistent));

        internal CAwaitable Sync(bool persistable = true)
        {
            var awaitable = new CAwaitable();

            var action = new CAction(awaitable.SignalCompletion, persistable);
            _callbacks.Add(action);

            return awaitable;
        }
    }
}
