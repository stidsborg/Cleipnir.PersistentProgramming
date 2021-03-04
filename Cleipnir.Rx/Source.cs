using System.Collections.Generic;

namespace Cleipnir.Rx
{
    public class Source<T> : Stream<T>
    {
        public Source() { }

        private Source(IReadOnlyDictionary<string, object> sd) : base(sd) { }

        public void Emit(T @event) => Notify(@event);

        public override void Dispose()
        {
            //Todo notify awaiters that no more events will be issued
        }

        private static Source<T> Deserialize(IReadOnlyDictionary<string, object> sd) => new Source<T>(sd);
    }
}
