using System.Linq;
using System.Text;
using Cleipnir.ExecutionEngine;
using Cleipnir.ExecutionEngine.Api;
using Cleipnir.GroupNetworkCommunication;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.ObjectDB.TaskAndAwaitable.StateMachine;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.GroupCommunication
{
    [TestClass]
    public class MessageQueueTests
    {
        [TestMethod]
        public void EmptyMessageQueueCanBeSerializeAndDeserialized()
        {
            var storage = new InMemoryStorageEngine();
            var c = ExecutionEngineFactory.StartNew(storage);
            
            c.Schedule(() =>
            {
                var q = new MessageQueue();
                Roots.Entangle(q);
            });

            c.Sync().Wait();
            
            c.Dispose();

            c = ExecutionEngineFactory.Continue(storage);
            c.Schedule(Roots.Resolve<MessageQueue>);
            c.Sync().Wait();
        }
        
        [TestMethod]
        public void NonEmptyMessageQueueCanBeSerializeAndDeserialized()
        {
            var storage = new InMemoryStorageEngine();
            var c = ExecutionEngineFactory.StartNew(storage);
            
            c.Schedule(() =>
            {
                var q = new MessageQueue();
                var hello = new ImmutableByteArray(Encoding.UTF8.GetBytes("hello"));
                var helloTask = new CTask();
                var world = new ImmutableByteArray(Encoding.UTF8.GetBytes("world"));
                var worldTask = new CTask();
                q.Add(hello, helloTask);
                q.Add(world, worldTask);
                Roots.Entangle(q);
                Roots.Entangle(new CImmutableList<CTask>(new [] {helloTask, worldTask}));
            });

            c.Sync().Wait();
            
            c.Dispose();

            c = ExecutionEngineFactory.Continue(storage);
            c.Schedule(() =>
            {
                var queue = Roots.Resolve<MessageQueue>();
                var elms = queue.GetAll().ToArray();
                elms.Length.ShouldBe(2);
                var s1 = Encoding.UTF8.GetString(elms[0].Array);
                s1.ShouldBe("hello");
                var s2 = Encoding.UTF8.GetString(elms[1].Array);
                s2.ShouldBe("world");
                
                Roots.Resolve<CImmutableList<CTask>>()[0].SignalCompletion();
            }).Wait();
            
            c.Sync().Wait();
            c.Dispose();
            
            c = ExecutionEngineFactory.Continue(storage);
            c.Schedule(() =>
            {
                var queue = Roots.Resolve<MessageQueue>();
                var elms = queue.GetAll().ToArray();
                elms.Length.ShouldBe(1);
                var s1 = Encoding.UTF8.GetString(elms[0].Array);
                s1.ShouldBe("world");
                
                Roots.Resolve<CImmutableList<CTask>>()[1].SignalCompletion();
            }).Wait();
            
            c.Sync().Wait();
            c.Dispose();
            
            c = ExecutionEngineFactory.Continue(storage);
            c.Schedule(() =>
            {
                var queue = Roots.Resolve<MessageQueue>();
                var elms = queue.GetAll().ToArray();
                elms.Length.ShouldBe(0);
            }).Wait();
            
            c.Sync().Wait();
            c.Dispose();
        }
    }
}