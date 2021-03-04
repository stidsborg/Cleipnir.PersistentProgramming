using System.Collections.Generic;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Api;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;

namespace Cleipnir.Rx.ExecutionEngine
{
    public class ScheduleOnNextEventSubscription<T> : IPersistable
    {
        private Stream<T> Stream { get; }
        public CAwaitable<T> Awaitable { get; }

        private bool _persistable;

        private bool _serialized;

        public ScheduleOnNextEventSubscription(Stream<T> stream, bool persistable)
        {
            _persistable = persistable;
            Stream = stream;
            Awaitable = new CAwaitable<T>();
            Stream.Subscribe(this, HandleNextEvent);
        }

        private ScheduleOnNextEventSubscription(Stream<T> stream, CAwaitable<T> awaitable, bool persistable)
        {
            Stream = stream;
            Awaitable = awaitable;

            _persistable = persistable;

            _serialized = true;
        }

        private void HandleNextEvent(T next)
        {
            var handlerAndEvent = new HandlerAndEvent<T>(Awaitable.SignalCompletion, next);
            Scheduler.Schedule(handlerAndEvent.Deliver, _persistable);
        } 

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_serialized) return;
            _serialized = true;

            sd.Set(nameof(Stream), Stream);
            sd.Set(nameof(Awaitable), Awaitable);
            sd.Set(nameof(_persistable), _persistable);
        }

        private static ScheduleOnNextEventSubscription<T> Deserialize(IReadOnlyDictionary<string, object> sd)
            => new ScheduleOnNextEventSubscription<T>(
                sd.Get<Stream<T>>(nameof(Stream)),
                sd.Get<CAwaitable<T>>(nameof(Awaitable)),
                sd.Get<bool>(nameof(_persistable))
            );
    }
}
