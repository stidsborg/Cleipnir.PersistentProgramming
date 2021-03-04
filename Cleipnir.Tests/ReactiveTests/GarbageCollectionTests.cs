using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.Rx;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ReactiveTests
{
    [TestClass]
    public class GarbageCollectionTests
    {
        [TestMethod]
        public void StreamOperatorsAreGarbageCollectedOnSingleSubscriptionDisposal()
        {
            var storage = new InMemoryStorageEngine();
            var source = new Source<int>();
            var staticNonPersistable = new StaticAndNonStaticValue();
            staticNonPersistable.SetUpSubscription(source);

            source.Emit(25);
            staticNonPersistable.Value.ShouldBe(25);
            StaticAndNonStaticValue.StaticValue.ShouldBe(25);

            var store = ObjectStore.New(storage);

            store.Entangle(source);
            store.Entangle(staticNonPersistable);
            store.Persist();

            store = ObjectStore.Load(storage, true, new MockScheduler());
            source = store.Resolve<Source<int>>();
            staticNonPersistable = store.Resolve<StaticAndNonStaticValue>();

            source.Emit(50);
            staticNonPersistable.Value.ShouldBe(50);
            StaticAndNonStaticValue.StaticValue.ShouldBe(50);
            staticNonPersistable.Subscription1.Dispose();

            source.Emit(100);
            staticNonPersistable.Value.ShouldBe(50);
            StaticAndNonStaticValue.StaticValue.ShouldBe(50);
        }

        [TestMethod]
        public void StreamOperatorsAreNotGarbageCollectedOnSubscriptionDisposalWhenOtherSubscriptionsExist()
        {
            var storage = new InMemoryStorageEngine();
            var source = new Source<int>();
            var staticNonPersistable = new StaticAndNonStaticValue();
            staticNonPersistable.SetUpSubscription(source);
            staticNonPersistable.SetUpSecondSubscription();

            source.Emit(25);
            staticNonPersistable.Value.ShouldBe(25);
            StaticAndNonStaticValue.StaticValue.ShouldBe(25);

            var store = ObjectStore.New(storage);

            store.Entangle(source);
            store.Entangle(staticNonPersistable);
            store.Persist();

            store = ObjectStore.Load(storage, true, new MockScheduler());
            source = store.Resolve<Source<int>>();
            staticNonPersistable = store.Resolve<StaticAndNonStaticValue>();

            source.Emit(50);
            staticNonPersistable.Value.ShouldBe(50);
            StaticAndNonStaticValue.StaticValue.ShouldBe(50);
            staticNonPersistable.Subscription1.Dispose();

            source.Emit(100);
            staticNonPersistable.Value.ShouldBe(100);
            StaticAndNonStaticValue.StaticValue.ShouldBe(100);

            store.Entangle(source);
            store.Entangle(staticNonPersistable);
            store.Persist();

            store = ObjectStore.Load(storage, true, new MockScheduler());
            source = store.Resolve<Source<int>>();
            staticNonPersistable = store.Resolve<StaticAndNonStaticValue>();

            source.Emit(150);
            staticNonPersistable.Value.ShouldBe(150);
            StaticAndNonStaticValue.StaticValue.ShouldBe(150);
            staticNonPersistable.Subscription2.Dispose();

            source.Emit(200);
            staticNonPersistable.Value.ShouldBe(150);
            StaticAndNonStaticValue.StaticValue.ShouldBe(150);
        }

        private class StaticAndNonStaticValue : IPersistable
        {
            public static int StaticValue { get; set; }
            public int SetStaticValue(int value) => StaticValue = value;

            public int Value { get; set; }
            public void SetValue(int i) => Value = i;

            public IDisposable Subscription1;
            public IDisposable Subscription2;

            private Stream<int> _selectedStream;

            public void SetUpSubscription(Stream<int> s) => Subscription1 = (_selectedStream = s.Select(SetStaticValue)).CallOnEvent(SetValue);
            public void SetUpSecondSubscription() => Subscription2 = _selectedStream.CallOnEvent(SetValue);

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(Value), Value);
                sd.Set(nameof(Subscription1), helper.GetReference(Subscription1));
                sd.Set(nameof(Subscription2), helper.GetReference(Subscription2));
            }

            private static StaticAndNonStaticValue Deserialize(IReadOnlyDictionary<string, object> sd)
            {
                var instance = new StaticAndNonStaticValue { Value = sd.Get<int>(nameof(Value)) };

                sd.ResolveReference<IDisposable>(nameof(Subscription1), s => instance.Subscription1 = s);
                sd.ResolveReference<IDisposable>(nameof(Subscription2), s => instance.Subscription2 = s);

                return instance;
            }
        }

        [TestMethod]
        public void PinnedOperatorsAreNotGarbageCollected()
        {
            var storage = new InMemoryStorageEngine();
            
            var source = new Source<int>();
            var valueHolder = new ValueHolder();

            var stream = source.Do(valueHolder.SetValue).Pin();
            var subscriptionHolder = new SubscriptionHolder(stream);

            source.Emit(25);
            
            valueHolder.Value.ShouldBe(25);
            subscriptionHolder.Value.ShouldBe(25);

            var store = ObjectStore.New(storage);

            store.Entangle(source);
            store.Entangle(valueHolder);
            store.Entangle(subscriptionHolder);
            store.Persist();

            store = ObjectStore.Load(storage, true, new MockScheduler());
            source = store.Resolve<Source<int>>();
            valueHolder = store.Resolve<ValueHolder>();
            subscriptionHolder = store.Resolve<SubscriptionHolder>();
            source.Emit(100);

            valueHolder.Value.ShouldBe(100);
            subscriptionHolder.Value.ShouldBe(100);

            subscriptionHolder.Subscription.Dispose();
            
            source.Emit(200);

            valueHolder.Value.ShouldBe(200);
            subscriptionHolder.Value.ShouldBe(100);

            store.Persist();

            store = ObjectStore.Load(storage, true, new MockScheduler());
            source = store.Resolve<Source<int>>();
            valueHolder = store.Resolve<ValueHolder>();
            subscriptionHolder = store.Resolve<SubscriptionHolder>();
            source.Emit(300);

            valueHolder.Value.ShouldBe(300);
            subscriptionHolder.Value.ShouldBe(100);
        }

        private class ValueHolder : IPersistable
        {
            public int Value { get; set; }

            public void SetValue(int value) => Value = value;

            public void Serialize(StateMap sd, SerializationHelper helper)
                => sd.Set(nameof(Value), Value);

            private static ValueHolder Deserialize(IReadOnlyDictionary<string, object> sd)
                => new ValueHolder() {Value = sd.Get<int>(nameof(Value))};
        }

        private class SubscriptionHolder : IPersistable
        {
            public int Value { get; set; }
            public IDisposable Subscription { get; private set; }
            public SubscriptionHolder(Stream<int> s) 
                => Subscription = s.Do(SetValue).CallOnEvent(SetValue);

            private SubscriptionHolder(int value) => Value = value;

            private void SetValue(int i) => Value = i;

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
               sd.Set(nameof(Subscription), helper.GetReference(Subscription));
               sd.Set(nameof(Value), Value);
            }

            private static SubscriptionHolder Deserialize(IReadOnlyDictionary<string, object> sd)
            {
                var instance = new SubscriptionHolder(sd.Get<int>(nameof(Value)));
                sd.ResolveReference<IDisposable>(nameof(Subscription), s => instance.Subscription = s);
                return instance;
            }
        }
    }
}
