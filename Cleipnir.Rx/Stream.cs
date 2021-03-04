using System;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.Persistency.Persistency;

namespace Cleipnir.Rx
{
    public abstract class Stream<T> : IDisposable, IPersistable
    {
        private CDictionary<object, Action<T>> Observers { get; set; }

        protected int NumberOfObservers => Observers.Count;

        private bool _serialized;

        protected Stream() => Observers = new CDictionary<object, Action<T>>();

        protected Stream(IReadOnlyDictionary<string, object> sd)
        {
            var reference = sd.Get<Reference>(nameof(Observers));
            reference.DoWhenResolved<CDictionary<object, Action<T>>>(d => Observers = d);
        }

        protected virtual void Notify(T @event)
        {
            foreach (var observer in Observers.Values.ToList())
                observer(@event);
        }

        internal virtual void Subscribe(object subscriber, Action<T> onNext) 
            => Observers[subscriber] = onNext;

        internal virtual void Unsubscribe(object subscriber) => Observers.Remove(subscriber);

        public Stream<TNewStream> DecorateStream<TNewStream>(IPersistableOperator<T, TNewStream> @operator) 
            => new StreamOperator<T, TNewStream>(this, @operator);

        public Stream<TNewStream> OfType<TNewStream>() where TNewStream : T => this
            .Where(t => t is TNewStream)
            .Select(tNew => (TNewStream) (object) tNew);

        public abstract void Dispose();

        public virtual void Serialize(StateMap sd, SerializationHelper helper)
        {
            if (_serialized) return;
            _serialized = true;

            var reference = helper.GetReference(Observers);
            sd.Set(nameof(Observers), reference);
        }
    }
}
