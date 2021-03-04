using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.Rx
{
    public class CallOnEventSubscription<T> : IPersistable, IDisposable
    {
        public Action<T> OnNext { get; set; }
        public Stream<T> Stream { get; }

        private bool _isSerialized;

        public CallOnEventSubscription(Stream<T> stream, Action<T> onNext)
        {
            Stream = stream;
            OnNext = onNext;
            stream.Subscribe(this, onNext);
        } 

        private CallOnEventSubscription(Stream<T> stream)
        {
            Stream = stream;
            _isSerialized = true;
        } 

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_isSerialized) return;
            _isSerialized = true;

            sd.Set(nameof(OnNext), helper.GetReference(OnNext));
            sd.Set(nameof(Stream), Stream);
        }

        private static CallOnEventSubscription<T> Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            var instance = new CallOnEventSubscription<T>(sd.Get<Stream<T>>(nameof(Stream))) {_isSerialized = true};
            
            sd.ResolveReference<Action<T>>(nameof(OnNext), onNext => instance.OnNext = onNext);

            return instance;
        }

        public void Dispose() => Stream.Unsubscribe(this);
    }
}
