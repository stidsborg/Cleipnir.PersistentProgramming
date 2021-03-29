using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ObjectStore
{
    [TestClass]
    public class DelegatePersistenceTests
    {
        private static List<string> _messages;
        
        [TestMethod]
        public void CanSerializeAndDeserializeActionWithNoParameters()
        {
            _messages = new List<string>();

            Action a = SayHallo;

            var storageEngine = new InMemoryStorageEngine();
            var os = ObjectStore2.New(storageEngine);
            os.Roots.Entangle(a);
            
            os.Persist();

            os = ObjectStore2.Load(storageEngine);
            a = os.Roots.Find<Action>();
            a();
            
            _messages.Count.ShouldBe(1);
            _messages[0].ShouldBe("hello");
        }
        
        private static void SayHallo() {
            _messages.Add("hello");
        }
        
        [TestMethod]
        public void CanSerializeAndDeserializeActionWithOneParameters()
        {
            _messages = new List<string>();

            Action<string> a = Say;

            var storageEngine = new InMemoryStorageEngine();
            var os = ObjectStore2.New(storageEngine);
            os.Roots.Entangle(a);
            
            os.Persist();

            os = ObjectStore2.Load(storageEngine);
            a = os.Roots.Find<Action<string>>();
            a("hello world");
            
            _messages.Count.ShouldBe(1);
            _messages[0].ShouldBe("hello world");
        }
        
        private static void Say(string message) {
            _messages.Add(message);
        }
    }
}