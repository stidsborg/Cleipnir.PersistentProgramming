using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;

namespace Cleipnir.Rx
{
    public class StreamOperator<TIn, TOut> : Stream<TOut>
    {
        private Stream<TIn> _inner;

        private readonly IPersistableOperator<TIn, TOut> _ipo;
        private bool _serialized;

        internal StreamOperator(Stream<TIn> inner, IPersistableOperator<TIn, TOut> ipo)
        {
            _inner = inner;
            _ipo = ipo;
        }

        internal StreamOperator(Stream<TIn> inner, IPersistableOperator<TIn, TOut> ipo, IReadOnlyDictionary<string, object> sd) : base(sd)
        {
            _inner = inner;
            _ipo = ipo;
        }

        private void Handle(TIn @event) => _ipo.Operator(@event, Notify);

        internal override void Subscribe(object subscriber, Action<TOut> onNext)
        {
            if (NumberOfObservers == 0)
                _inner.Subscribe(this, Handle);

            base.Subscribe(subscriber, onNext);
        }

        internal override void Unsubscribe(object subscriber)
        {
            base.Unsubscribe(subscriber);

            if (NumberOfObservers == 0)
                _inner.Unsubscribe(this);
        }

        public override void Dispose()
        {
            _inner?.Unsubscribe(this);
            _inner = null;
        }

        public override void Serialize(StateMap sd, SerializationHelper helper)
        {
            base.Serialize(sd, helper);

            if (_serialized) return;
            _serialized = true;

            sd.Set(nameof(_inner), _inner);
            sd.Set(nameof(_ipo), _ipo);
        }

        internal static StreamOperator<TIn, TOut> Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            var inner = (Stream<TIn>) sd[nameof(_inner)];
            var @operator = (IPersistableOperator<TIn, TOut>) sd[nameof(_ipo)];

            return new StreamOperator<TIn, TOut>(inner, @operator, sd) { _serialized = true };
        }
    }
}
