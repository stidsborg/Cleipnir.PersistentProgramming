using System;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ObjectStore.Persistence
{
    [TestClass]
    public class ExceptionPersistenceTests
    {
        [TestMethod]
        public void SerializeAndDeserializeExceptions()
        {
            var storageEngine = new InMemoryStorageEngine();
            var os = ObjectStore2.New(storageEngine);
            
            var innerException = new InvalidCastException("cast was not possible");
            var outerException = new ArgumentException("argument was wrong", innerException);
            
            os.Roots.Entangle(outerException);
            
            os.Persist();

            os = ObjectStore2.Load(storageEngine);
            var e = os.Roots.Find<ArgumentException>();
            
            e.Message.ShouldBe("argument was wrong");
            var ei = (InvalidCastException) e.InnerException;
            ei.Message.ShouldBe("cast was not possible");
        }
    }
}