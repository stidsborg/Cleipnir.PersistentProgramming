using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.Helpers;
using Cleipnir.NetworkCommunication;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using static Cleipnir.Helpers.FunctionalExtensions;

namespace Cleipnir.Tests.NetworkCommunication
{
    [TestClass]
    public class OutgoingCommunicationTests
    {
        [TestMethod]
        public void OutgoingConnectionSendsMessageSuccessfully()
        {
            const string hostname = "127.0.0.1";
            const int port = 10100;
            const string connectionIdentifier = "1";

            var server = new SingleConnectionServer();
            server.StartListening();
            
            var engine = ExecutionEngineFactory.StartNew(new InMemoryStorageEngine());

            engine.Schedule(() =>
            {
                var unackedMessageQueue = new OutgoingMessageQueue();
                var deliverer = new OutgoingMessageDeliverer(hostname, port, connectionIdentifier, unackedMessageQueue);
                
                Roots.Entangle(deliverer);
                Roots.Entangle(unackedMessageQueue);
                deliverer.Send("HELLO WORLD".GetUtf8Bytes());
            });
            
            Thread.Sleep(1_000);
            
            server.GetNodeIdentifier().ShouldBe("1");
            server.GetReceivedMessages()[0].Item2.ShouldBe("HELLO WORLD");
            
            var unackeds = engine.Schedule(() =>
            {
                var q = Roots.Resolve<OutgoingMessageQueue>();
                return q.GetSyncedUnackedMessages().ToArray();
            }).Result;
            
            unackeds.Length.ShouldBe(1);
            unackeds[0].Item1.ShouldBe(0);
            unackeds[0].Item2.Array.ToUtf8String().ShouldBe("HELLO WORLD");

            server.AckUntil(0);
            
            Thread.Sleep(1_000);
            
            unackeds = engine.Schedule(() =>
            {
                var q = Roots.Resolve<OutgoingMessageQueue>();
                return q.GetSyncedUnackedMessages().ToArray();
            }).Result;
            
            unackeds.Length.ShouldBe(0);
        }
        
        //todo deactivated as it crashed the test framework [TestMethod]
        public void OutgoingMessageIsResetOnSocketReset()
        {
            const string hostname = "127.0.0.1";
            const int port = 10100;
            const string connectionIdentifier = "1";

            var server = new SingleConnectionServer();
            server.StartListening();
            
            var engine = ExecutionEngineFactory.StartNew(new InMemoryStorageEngine());

            engine.Schedule(() =>
            {
                var unackedMessageQueue = new OutgoingMessageQueue();
                var deliverer = new OutgoingMessageDeliverer(hostname, port, connectionIdentifier, unackedMessageQueue);
                
                Roots.Entangle(deliverer);
                Roots.Entangle(unackedMessageQueue);
                deliverer.Send("HELLO WORLD".GetUtf8Bytes());
            });
            
            Thread.Sleep(1_000);
            
            server.DisposeConnectedSocket();
            
            Thread.Sleep(1_000);
            
            server.GetNodeIdentifier().ShouldBe("1");
            server.GetReceivedMessages().Count.ShouldBe(2);
        }
    }

    internal class SingleConnectionServer : IDisposable
    {
        private const int Port = 10100;
        private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        private volatile bool _disposed;

        private Socket _newSocket;

        private string _nodeIdentifier;
        private readonly List<Tuple<int, string>> _receivedMessages = new();

        private readonly object _sync = new object();

        public async void StartListening()
        {
            _socket.Bind(IPEndPoint.Parse($"127.0.0.1:{Port}"));
            _socket.Listen();

            while (!_disposed)
            {
                var socket = await _socket.AcceptAsync();

                var received = 0;

                var headerBuffer = new byte[4];
                while (received < 4)
                    received += await socket.ReceiveAsync(
                        new ArraySegment<byte>(headerBuffer, received, headerBuffer.Length - received),
                        SocketFlags.None
                    );

                var identifierLength = BitConverter.ToInt32(headerBuffer);
                var identifierBuffer = new byte[identifierLength];

                received = 0;
                while (received < identifierBuffer.Length)
                    received += await socket.ReceiveAsync(new ArraySegment<byte>(identifierBuffer, received, identifierLength - received), 
                        SocketFlags.None);

                var identifier = Encoding.UTF8.GetString(identifierBuffer);
                lock (_sync)
                    _nodeIdentifier = identifier;

                _ = Task.Run(() => HandleIncomingConnection(socket));
            }
        }

        private async void HandleIncomingConnection(Socket socket)
        {
            try
            {
                lock (_sync)
                    _newSocket = socket;

                while (!_disposed)
                {
                    var headerBuffer = new byte[8];
                    while (true)
                    {
                        var readSoFar = 0;

                        while (readSoFar < 8)
                            readSoFar += await socket.ReceiveAsync(
                                new ArraySegment<byte>(headerBuffer, readSoFar, headerBuffer.Length - readSoFar),
                                SocketFlags.None
                            );

                        var messageIndex = BitConverter.ToInt32(headerBuffer);
                        var length = BitConverter.ToInt32(headerBuffer[4..]);

                        readSoFar = 0;
                        var buffer = new byte[length];
                        while (readSoFar < length)
                            readSoFar += await socket.ReceiveAsync(
                                new ArraySegment<byte>(buffer, readSoFar, length - readSoFar),
                                SocketFlags.None
                            );

                        lock (_sync)
                        {
                            var s = Encoding.UTF8.GetString(buffer);
                            _receivedMessages.Add(Tuple.Create(messageIndex, s));
                        }
                    }
                }
            } catch (Exception _) {}
        }

        public void DisposeConnectedSocket()
        {
            lock (_sync)
                SafeTry(_newSocket.Dispose);  
        } 

        public void AckUntil(int index) =>_newSocket.Send(BitConverter.GetBytes(index));

        public string GetNodeIdentifier()
        {
            lock (_sync)
                return _nodeIdentifier;
        }

        public List<Tuple<int, string>> GetReceivedMessages()
        {
            lock (_sync)
                return _receivedMessages.ToList();
        }

        public void Dispose()
        {
            _disposed = true;
            SafeTry(_socket.Dispose);  
        } 
    }
}