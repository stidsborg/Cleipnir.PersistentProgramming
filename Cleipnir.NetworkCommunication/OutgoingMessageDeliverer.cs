using System.Collections.Generic;
using System.Net;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Api;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.NetworkCommunication
{
    internal class OutgoingMessageDeliverer : IPersistable
    {
        private readonly string _hostName;
        private readonly int _port;
        private readonly string _identifier;
        private readonly IPEndPoint _endPoint;
        
        private readonly OutgoingMessageQueue _unackedQueue;

        private OutgoingConnection _connection;

        public OutgoingMessageDeliverer(string hostName, int port, string connectionIdentifier, OutgoingMessageQueue unackedQueue)
        {
            _hostName = hostName;
            _port = port;
            _endPoint = IPEndPoint.Parse($"{hostName}:{port}");
            
            _identifier = connectionIdentifier;
            _unackedQueue = unackedQueue;

            Scheduler.Schedule(CreateNewMessageSender, false);
        }

        public void Send(byte[] msg)
        {
            var array = new ImmutableByteArray(msg);
            var msgIndex = _unackedQueue.Enqueue(array);
            Sync.AfterNext(() => _connection.Send(msgIndex, array), false);
        }

        private void CreateNewMessageSender()
        {
            _connection = new OutgoingConnection(_endPoint, _identifier, _unackedQueue);
            _connection.Start();
        }

        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd.Set(nameof(_hostName), _hostName);
            sd.Set(nameof(_port), _port);
            sd.Set(nameof(_identifier), _identifier);
            sd.Set(nameof(_unackedQueue), _unackedQueue);
        }

        private static OutgoingMessageDeliverer Deserialize(IReadOnlyDictionary<string, object> sd)
            => new OutgoingMessageDeliverer(
                sd.Get<string>(nameof(_hostName)),
                sd.Get<int>(nameof(_port)),
                sd.Get<string>(nameof(_identifier)),
                sd.Get<OutgoingMessageQueue>(nameof(_unackedQueue))
            );
    }
}
