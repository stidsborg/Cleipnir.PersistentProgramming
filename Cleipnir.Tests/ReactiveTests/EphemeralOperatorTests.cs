using Cleipnir.ObjectDB;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ReactiveTests
{
    [TestClass]
    public class EphemeralOperatorTests
    {
        [TestMethod]
        public void DependentOperatorsAreNotDeserializedForEphemeralOperator()
        {
            var storage = new InMemoryStorageEngine();
            var os = ObjectStore.New(storage);

            var source = new Source<int>();
            var valueHolder = new ValueHolder<int>();
            source.Ephemeral().CallOnEvent(valueHolder.SetValue);

            source.Emit(10);
            valueHolder.Value.ShouldBe(10);

            os.Entangle(source);
            os.Entangle(valueHolder);

            os.Persist();

            os = ObjectStore.Load(storage, true);
            source = os.Resolve<Source<int>>();
            valueHolder = os.Resolve<ValueHolder<int>>();

            source.Emit(20);
            valueHolder.Value.ShouldBe(10);
        }
    }
}
