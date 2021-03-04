using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;
using static Cleipnir.Helpers.FunctionalExtensions;

namespace Cleipnir.Rx
{
    public class StreamQueue<T> : IPersistable, IDisposable
    {
        private Stream<T> _stream;
        private CAwaitable<IEnumerable<T>> _awaitable;

        private CAppendOnlyList<T> _toDeliver;
        private bool _disposed;

        internal StreamQueue(Stream<T> stream)
        {
            stream.Subscribe(this, Handle);

            _awaitable = null;
            _toDeliver = new CAppendOnlyList<T>();
        }

        private StreamQueue() {}

        public CAwaitable<IEnumerable<T>> Dequeue
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(StreamQueue<T>));

                _awaitable ??= new CAwaitable<IEnumerable<T>>();
                var toReturn = _awaitable;

                if (_toDeliver.Count > 0) SignalAwaitable();
                
                return toReturn;
            }
        }

        private void Handle(T next)
        {
            _toDeliver.Add(next);

            if (_awaitable != null) SignalAwaitable();
        }

        private void SignalAwaitable()
        {
            _awaitable.SignalCompletion(_toDeliver);
            _awaitable = null;
            _toDeliver = new CAppendOnlyList<T>();
        }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd.Set(nameof(_awaitable), _awaitable);
            sd.Set(nameof(_toDeliver), _toDeliver);
            sd.Set(nameof(_stream), _stream);
            sd.Set(nameof(_disposed), _disposed);
        }

        private static StreamQueue<T> Deserialize(IReadOnlyDictionary<string, object> sd)
            => new StreamQueue<T>
            {
                _stream = sd.Get<Stream<T>>(nameof(_stream)),
                _awaitable = sd.Get<CAwaitable<IEnumerable<T>>>(nameof(_awaitable)),
                _toDeliver = sd.Get<CAppendOnlyList<T>>(nameof(_toDeliver)),
                _disposed = sd.Get<bool>(nameof(_disposed))
            };

        public void Dispose()
        {
            _disposed = true;
            SafeTry(() => _awaitable?.SignalThrownException(new ObjectDisposedException(nameof(StreamQueue<T>))));
            _awaitable = null;
            _stream?.Unsubscribe(this);
            _stream = null;
        }
    }

    public static class QueueExtensions
    {
        public static StreamQueue<T> ToQueue<T>(this Stream<T> s) => new StreamQueue<T>(s);
    }
}
