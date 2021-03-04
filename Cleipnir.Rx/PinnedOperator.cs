using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;

namespace Cleipnir.Rx
{
    public class PinnedOperator<T> : Stream<T>
    {
        private Stream<T> Inner { get; }

        public PinnedOperator(Stream<T> inner)
        {
            Inner = inner;
            Inner.Subscribe(this, Notify);
        }

        private PinnedOperator(Stream<T> inner, IReadOnlyDictionary<string, object> sd) 
            : base(sd) => Inner = inner;

        public override void Dispose() => Inner.Unsubscribe(this);

        public override void Serialize(StateMap sd, SerializationHelper helper)
        {
            base.Serialize(sd, helper);
            sd.Set(nameof(Inner), Inner);
        }

        private static PinnedOperator<T> Deserialize(IReadOnlyDictionary<string, object> sd)
            => new PinnedOperator<T>(sd.Get<Stream<T>>(nameof(Inner)), sd);
    }
}
