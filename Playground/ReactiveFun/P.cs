using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Api;
using Cleipnir.ExecutionEngine.Providers;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.Rx;
using Cleipnir.Rx.ExecutionEngine;
using Cleipnir.StorageEngine.InMemory;

namespace Playground.ReactiveFun
{
    public static class P
    {
        public static void Do()
        {
            var storageEngine = new InMemoryStorageEngine();
            var executionEngine = ExecutionEngineFactory.StartNew(storageEngine);

            executionEngine.Schedule(() =>
            {
                var network = new Network();
                var source = new Source<object>();
                Roots.Entangle(source);
                
                var proposer = new PaxosProposer(source, network);
                _ = proposer.StartRound("hello", 0);

                _ = OutsideFlow(source, network);
            });
        }

        private static async CTask OutsideFlow(Source<object> messages, Network network)
        {
            await Sleep.Until(1000);
            var accepts = network.OutgoingMessages.OfType<Accept>().Next();
            
            messages.Emit(new Promise() {Ballot = 0});
            messages.Emit(new Promise() {Ballot = 0});
            messages.Emit(new Promise() {Ballot = 0});

            await accepts;

            await Sleep.Until(1000);
            messages.Emit(new Accepted() {});
            messages.Emit(new Accepted() {});
            messages.Emit(new Accepted() {});
        }

        public class PaxosProposer : IPersistable
        {
            private Stream<object> Messages { get; }
            public Network Network { get; }

            private const int Quorum = 3;

            public PaxosProposer(Stream<object> messages, Network network)
            {
                Messages = messages;
                Network = network;
            }

            public async CTask DecideProposal(string proposal)
            {
                throw new NotImplementedException();
            }

            public async CTask StartRound(string proposal, int ballot)
            {
                Network.Broadcast(new Prepare {Ballot = ballot});
                var promises = await Messages 
                    .OfType<Promise>()
                    .Scan(new CAppendOnlyList<Promise>(), (l, p) => { l.Add(p); return l; })
                    .Where(l => l.Count == Quorum)
                    .Next();

                var toPropose = promises
                    .Append(new Promise {PromisedBallot = -1, PromisedValue = proposal})
                    .Where(p => p.PromisedBallot != null)
                    .OrderByDescending(p => p.PromisedBallot)
                    .First()
                    .PromisedValue;

                Network.Broadcast(new Accept() {Ballot = ballot, Value = toPropose});
                await Messages
                    .OfType<Accepted>()
                    .WaitFor(Quorum, true);

                Console.WriteLine("COMPLETED!!!");
            }

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd[nameof(Messages)] = Messages;
                sd[nameof(Network)] = Network;
            }

            private static PaxosProposer Deserialize(IReadOnlyDictionary<string, object> sd)
                => throw new NotImplementedException();
        }

        public class Prepare : IPropertyPersistable
        {
            public int Ballot { get; set; }
        }

        public class Promise: IPropertyPersistable
        {
            public int Ballot { get; set; }
            public int? PromisedBallot { get; set; }
            public string PromisedValue { get; set; }
        }

        public class Accept : IPropertyPersistable
        {
            public int Ballot { get; set; }
            public string Value { get; set; }
        }
        public class Accepted : IPropertyPersistable 
        {}
    }

    public class Network : IPersistable
    {
        private CList<object> BroadcastMessages { get; init; }
        private readonly object _sync = new object();

        private Source<object> Source = new();
        public Stream<object> OutgoingMessages => Source;

        public Network() => BroadcastMessages = new();

        public void Broadcast(object msg)
        {
            lock (_sync)
                BroadcastMessages.Add(msg);
            
            Task.Run(() => Source.Emit(msg));
        }

        public List<object> GetAll()
        {
            lock (_sync)
                return BroadcastMessages.ToList();
        }
        
        public void Serialize(StateMap sd, SerializationHelper helper) => 
            sd[nameof(BroadcastMessages)] = BroadcastMessages;

        private static Network Deserialize(IReadOnlyDictionary<string, object> sd)
            => new Network() {BroadcastMessages = sd.Get<CList<object>>(nameof(BroadcastMessages))};
    }
}
