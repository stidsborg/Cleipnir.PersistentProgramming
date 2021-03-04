using System;
using System.Collections.Generic;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Api;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;

namespace Cleipnir.Rx.ExecutionEngine
{
    public static class StreamsLinq
    {
        // ** WAIT_FOR ** //
        public static CAwaitable<IEnumerable<T>> WaitFor<T>(this Stream<T> s, int count, bool persistable, TimeSpan? timeout = null)
            => new WaitForOperator<T>(count, s, timeout, persistable).Awaitable;

        private class WaitForOperator<T> : IPersistable
        {
            private readonly int _count;
            public CAwaitable<IEnumerable<T>> Awaitable { get; }
            private bool _completed;
            private Stream<T> _inner;
            private readonly CAppendOnlyList<T> _list;

            public WaitForOperator(int count, Stream<T> inner, TimeSpan? timeout, bool persistable)
            {
                _inner = inner;
                _count = count;
                Awaitable = new CAwaitable<IEnumerable<T>>(); //todo should the continuation be scheduled or not?
                _list = new CAppendOnlyList<T>();

                if (timeout != null)
                    Sleep.Until(timeout.Value, HandleTimeout, persistable);

                _inner.Subscribe(this, Handle);
            }

            private WaitForOperator(int count, CAwaitable<IEnumerable<T>> awaitable, Stream<T> inner, bool completed, CAppendOnlyList<T> list)
            {
                _count = count;
                Awaitable = awaitable;
                _completed = completed;
                _inner = inner;
                _list = list;
            }

            private void Handle(T t)
            {
                _list.Add(t);

                if (_list.Count < _count || _completed) return;

                Awaitable.SignalCompletion(_list);
                _completed = true;

                _inner?.Unsubscribe(this);
                _inner = null;
            }

            private void HandleTimeout()
            {
                if (_completed) return;

                Awaitable.SignalThrownException(new TimeoutException());
                _completed = true;

                _inner?.Unsubscribe(this);
                _inner = null;
            }

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(_list), _list);
                sd.Set(nameof(_count), _count);
                sd.Set(nameof(_inner), _inner);
                sd.Set(nameof(_completed), _completed);
                sd.Set(nameof(Awaitable), Awaitable);
            }

            internal static WaitForOperator<T> Deserialize(IReadOnlyDictionary<string, object> sd)
            {
                return new WaitForOperator<T>(
                    (int)sd[nameof(_count)],
                    (CAwaitable<IEnumerable<T>>)sd[nameof(Awaitable)],
                    (Stream<T>)sd[nameof(_inner)],
                    (bool)sd[nameof(_completed)],
                    sd.Get<CAppendOnlyList<T>>(nameof(_list))
                );
            }
        }

        // ** SCHEDULER OPERATOR ** //
        public static Stream<T> Schedule<T>(this Stream<T> s, bool persistable = true)
            => s.DecorateStream(new SchedulerOperator<T>(persistable));

        private class SchedulerOperator<T> : IPersistableOperator<T, T>
        {
            private readonly bool _persistable;

            public SchedulerOperator(bool persistable) => _persistable = persistable;

            public void Operator(T next, Action<T> notify)
            {
                Scheduler.Schedule(CActionWithSpecifiedParameters.Create(next, notify), _persistable);
            }

            public void Serialize(StateMap sd, SerializationHelper helper) =>
                sd.Set(nameof(_persistable), _persistable);

            private static SchedulerOperator<T> Deserialize(IReadOnlyDictionary<string, object> sd)
                => new SchedulerOperator<T>(sd.Get<bool>(nameof(_persistable)));
        }
    }
}
