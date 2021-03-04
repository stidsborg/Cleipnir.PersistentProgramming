using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Api;
using Cleipnir.Helpers;
using Cleipnir.NetworkCommunication;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.StorageEngine.InMemory;
using Cleipnir.Tests.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.NetworkCommunicationTests
{
    //[TestClass]
    public class IncomingConnectionTests
    {
        //[TestMethod]
        public void SentMessageIsProcessed()
        {
            var hostname = "127.0.0.1";
            const int port = 10_000;
            
            var storage = new InMemoryStorageEngine();
            var scheduler1 = ExecutionEngineFactory.StartNew(storage);
            scheduler1.Schedule(() =>
            {
                var msgHandler = new MessageHandler();
                var connectionListener = new ConnectionServer(hostname, port, msgHandler.Handle);

                Roots.Entangle(msgHandler);
                Roots.Entangle(connectionListener);
            });

            var senderId = Guid.Empty;
            var msgSender = new TestMessageSender(hostname, port, senderId);
            msgSender.Connect();
            msgSender.SendMessage("HELLO".GetUtf8Bytes(), 0);
            Thread.Sleep(1000);

            var messageHandler = scheduler1.Resolve<MessageHandler>().Result;
            var messages = messageHandler.GetMessages();
            messages.Count.ShouldBe(1);
            messages[0].ShouldBe("HELLO");

            scheduler1.Do<ConnectionServer>(c => c.Shutdown());

            Thread.Sleep(1000);

            scheduler1.Dispose();
            scheduler1 = ExecutionEngineFactory.Continue(storage);
            messageHandler = scheduler1.Resolve<MessageHandler>().Result;
            Console.WriteLine(messageHandler);
            
            msgSender = new TestMessageSender(hostname, port, senderId);
            msgSender.Connect();
            msgSender.SendMessage("HELLO2".GetUtf8Bytes(), 1);
            Thread.Sleep(1000);

            messages = messageHandler.GetMessages();
            messages.Count.ShouldBe(2);
            messages[0].ShouldBe("HELLO");
            messages[1].ShouldBe("HELLO2");

            scheduler1.Dispose();
        }

        private string GetLocalIp()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);

            socket.Connect("8.8.8.8", 65530);
            var endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint.Address.ToString();
        }
    }

    internal class MessageHandler : IPersistable
    {
        private CAppendOnlyList<string> Messages { get; set; } = new CAppendOnlyList<string>();
        private readonly object _sync = new object();

        public void Handle(ImmutableByteArray msg)
        {
            var s = Encoding.UTF8.GetString(msg.Array);
            lock (_sync)
                Messages.Add(s);
        }

        public List<string> GetMessages()
        {
            lock (_sync)
                return Messages.ToList();
        }

        public void Serialize(StateMap sd, SerializationHelper helper)
            => sd.Set(nameof(Messages), Messages);

        private static MessageHandler Deserialize(IReadOnlyDictionary<string, object> sd)
            => new MessageHandler() {Messages = sd.Get<CAppendOnlyList<string>>(nameof(Messages))};
    }

    internal class TestMessageSender
    {
        private readonly Guid _connectionId;
        private readonly IPEndPoint _endPoint;
        private Socket _socket;

        public TestMessageSender(string hostName, int port, Guid connectionId)
        {
            _connectionId = connectionId;
            _endPoint = IPEndPoint.Parse($"{hostName}:{port}");
        }

        public void Connect()
        {
            _socket?.Dispose();
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(_endPoint);
            _socket.Send(_connectionId.ToByteArray());
        }

        public void SendMessage(byte[] msg, int index)
        {
            var arraySegments = new List<ArraySegment<byte>>();

            arraySegments.Add(BitConverter.GetBytes(index));
            arraySegments.Add(BitConverter.GetBytes(msg.Length));
            arraySegments.Add(msg);

            _socket.Send(arraySegments, SocketFlags.None);
        }
    }
}
