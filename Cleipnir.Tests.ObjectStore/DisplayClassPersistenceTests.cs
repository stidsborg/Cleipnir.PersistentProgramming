using System;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers.Persistable;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ObjectStore
{
    [TestClass]
    public class DisplayClassPersistenceTests
    {
        [TestMethod]
        public void SerializeAndDeserializeSimpleDisplayClass()
        {
            var storageEngine = new InMemoryStorageEngine();
            var os = ObjectStore2.New(storageEngine);

            var wrapper = new Wrapper() {Text = "World"};
            var greeter = wrapper.Greeter("Hello");
            
            os.Roots.Entangle(greeter);
            os.Persist();

            os = ObjectStore2.Load(storageEngine);
            greeter = os.Roots.Find<Func<string>>();
            greeter().ShouldBe("Hello World");
        }

        private class Wrapper : IPersistable2
        {
            public string Text { get; set; }

            public Func<string> Greeter(string prepend)
            {
                return () => prepend + " " + Text;
            }
            
            public void Serialize(Map2 m)
            {
                m[nameof(Text)] = Text;
            }

            private static Wrapper Deserialize(RMap rm)
            {
                return new Wrapper() {Text = rm.Get<string>(nameof(Text))};
            }
        }
    }
}