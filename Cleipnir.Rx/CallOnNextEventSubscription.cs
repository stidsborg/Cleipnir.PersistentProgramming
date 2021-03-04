using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;

namespace Cleipnir.Rx
{
    public class CallOnNextEventSubscription<T> : IPersistable
    {
        private Stream<T> Stream { get; }
        public CAwaitable<T> Awaitable { get; }

        private bool _serialized;

        public CallOnNextEventSubscription(Stream<T> stream)
        {
            Stream = stream;
            Awaitable = new CAwaitable<T>();
            Stream.Subscribe(this, HandleNextEvent);
        }

        private CallOnNextEventSubscription(Stream<T> stream, CAwaitable<T> awaitable)
        {
            Stream = stream;
            Awaitable = awaitable;
            _serialized = true;
        }

        private void HandleNextEvent(T next)
        {
            Awaitable.SignalCompletion(next);
            Stream.Unsubscribe(this); //todo !
        } 

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_serialized) return;
            _serialized = true;

            sd.Set(nameof(Stream), Stream);
            sd.Set(nameof(Awaitable), Awaitable);
        }

        private static CallOnNextEventSubscription<T> Deserialize(IReadOnlyDictionary<string, object> sd)
            => new CallOnNextEventSubscription<T>(
                sd.Get<Stream<T>>(nameof(Stream)),
                sd.Get<CAwaitable<T>>(nameof(Awaitable))
            );
    }
}
