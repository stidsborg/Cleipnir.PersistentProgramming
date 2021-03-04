using System;
using System.Collections.Generic;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Api;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.Rx.ExecutionEngine
{
    public class ScheduleOnEventSubscription<T> : IPersistable, IDisposable
    {
        private Action<T> OnNext { get; set; }
        private Stream<T> Stream { get; }

        private bool IsPersistable { get; }
        private bool _isSerialized;

        public ScheduleOnEventSubscription(Stream<T> stream, Action<T> onNext, bool isPersistable)
        {
            IsPersistable = isPersistable;
            OnNext = onNext;
            Stream = stream;

            stream.Subscribe(this, Schedule);
        }

        private ScheduleOnEventSubscription(Stream<T> stream, bool isPersistable)
        {
            IsPersistable = isPersistable;
            Stream = stream;
        }

        private void Schedule(T next)
        {
            var handlerAndEvent = new HandlerAndEvent<T>(OnNext, next);
            Scheduler.Schedule(handlerAndEvent.Deliver, IsPersistable);
        }

        public void Dispose() => Stream.Unsubscribe(this);

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_isSerialized) return;
            _isSerialized = true;

            sd.Set(nameof(OnNext), helper.GetReference(OnNext));
            sd.Set(nameof(Stream), Stream);
            sd.Set(nameof(IsPersistable), IsPersistable);
        }

        private static ScheduleOnEventSubscription<T> Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            var instance = new ScheduleOnEventSubscription<T>(
                    sd.Get<Stream<T>>(nameof(Stream)),
                    sd.Get<bool>(nameof(IsPersistable))
                )
                {_isSerialized = true};

            sd.ResolveReference<Action<T>>(nameof(OnNext), onNext => instance.OnNext = onNext);

            return instance;
        }
    }
}
