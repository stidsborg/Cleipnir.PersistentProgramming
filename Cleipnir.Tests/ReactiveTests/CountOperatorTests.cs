using Cleipnir.ObjectDB;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ReactiveTests
{
    [TestClass]
    public class CountOperatorTests
    {
        [TestMethod]
        public void CountOperatorTest()
        {
            var storage = new InMemoryStorageEngine();
            var os = ObjectStore.New(storage);
            
            var source = new Source<int>();
            var valueHolder = new ValueHolder<int>();
            source.Count().CallOnEvent(valueHolder.SetValue);

            source.Emit(0);
            valueHolder.Value.ShouldBe(1);
            
            source.Emit(0);
            valueHolder.Value.ShouldBe(2);
            
            os.Entangle(source);
            os.Entangle(valueHolder);
            
            os.Persist();
            
            os = ObjectStore.Load(storage);
            source = os.Resolve<Source<int>>();
            valueHolder = os.Resolve<ValueHolder<int>>();
            
            source.Emit(0);
            valueHolder.Value.ShouldBe(3);
        }
    }
}
