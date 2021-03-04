using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Api;
using Cleipnir.Helpers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;

namespace Cleipnir.NetworkCommunication
{
    public class IncomingMessageDeliverer : IPersistable
    {
        private readonly Dictionary<int, ImmutableByteArray> _preSyncToDeliver = new();
        private CDictionary<int, ImmutableByteArray> _postSyncToDeliver;

        private IncomingConnection _incomingConnection;

        private Engine Engine { get; }
        
        private Action<ImmutableByteArray> MessageListener { get; }

        public IncomingMessageDeliverer(
            Action<ImmutableByteArray> messageListener,
            CDictionary<int, ImmutableByteArray> toDeliver,
            int atIndex,
            Engine engine)
        {
            MessageListener = messageListener;
            _postSyncToDeliver = toDeliver;
            _postSyncAtIndex = _preSyncAtIndex= atIndex;
            Engine = engine;
        }
        
        private readonly object _sync = new();
        private int _preSyncAtIndex;
        private int _postSyncAtIndex;

        public void SetNewSocketConnection(Socket socket)
        {
            _incomingConnection?.Dispose();
            _incomingConnection = new IncomingConnection(socket, this);
        }
        
        public void EnqueueForDeliver(int msgIndex, byte[] bytes)
        {
            lock (_sync)
                _preSyncToDeliver[msgIndex] = new ImmutableByteArray(bytes);

            Engine.Sync(); //schedule sync
        }

        private void AckAtIndexToOtherSide()
        {
            var atIndex = _postSyncAtIndex;
            Task.Run(() => _incomingConnection.SendAtIndexToOtherSide(atIndex));
        }

        private void Deliver()
        {
            if (_postSyncToDeliver.Empty()) return;

            var keys = _postSyncToDeliver.Keys.OrderBy(_ => _).ToList();
            foreach (var key in keys)
                MessageListener(_postSyncToDeliver[key]);

            _preSyncAtIndex = keys.Last();
            _postSyncToDeliver = new();
            
            Sync.AfterNext(() => {}, false); //schedule synchronization
        }

        public void Shutdown() =>_incomingConnection.Dispose();

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            var newMessages = false;
            lock (_sync)
            {
                foreach (var entry in _preSyncToDeliver)
                {
                    if (entry.Key > _preSyncAtIndex && _postSyncToDeliver.ContainsKey(entry.Key))
                    {
                        _postSyncToDeliver[entry.Key] = entry.Value;
                        newMessages = true;
                    }
                }
                
                _preSyncToDeliver.Clear();

                if (_postSyncAtIndex < _preSyncAtIndex)
                {
                    _postSyncAtIndex = _preSyncAtIndex;
                    Scheduler.Schedule(AckAtIndexToOtherSide);
                }
            }
            
            if (newMessages)
                Scheduler.Schedule(Deliver, false);
            
            sd.Set("AtIndex", _postSyncAtIndex);
            sd.Set("ToDeliver", _postSyncToDeliver);
            sd.Set(nameof(MessageListener), MessageListener);
        }

        private static IncomingMessageDeliverer Deserialize(IReadOnlyDictionary<string, object> sd)
        {
            var atIndex = sd.Get<int>("AtIndex");
            var toDeliver = sd.Get<CDictionary<int, ImmutableByteArray>>("ToDeliver");
            var messageListener = sd.Get<Action<ImmutableByteArray>>(nameof(MessageListener));
            return new IncomingMessageDeliverer(messageListener, toDeliver, atIndex, Engine.Current);
        }
    }
}