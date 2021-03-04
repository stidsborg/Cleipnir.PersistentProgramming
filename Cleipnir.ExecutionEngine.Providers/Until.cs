using System;
using System.Collections.Generic;
using Cleipnir.ExecutionEngine.Api;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;

namespace Cleipnir.ExecutionEngine.Providers
{
    public static class Until
    {
        public static CAwaitable This(Func<bool> predicate, bool persistable = true)
        {
            var awaitable = new CAwaitable();
            var wrapper = new UntilWrapper(predicate, awaitable.SignalCompletion, persistable);
            Scheduler.Schedule(wrapper.TestCondition, persistable);
            
            return awaitable;
        }

        public static void This(Func<bool> predicate, Action callback, bool persistable)
        {
            _ = new UntilWrapper(predicate, callback, persistable);
        }

        private class UntilWrapper : IPersistable
        {
            private readonly Func<bool> _predicate;
            private readonly Action _callback;
            private readonly bool _persistable;

            private bool _serialized;

            public UntilWrapper(Func<bool> predicate, Action callback, bool persistable)
            {
                _predicate = predicate;
                _callback = callback;
                _persistable = persistable;
            }

            public void TestCondition()
            {
                if (_predicate())
                    _callback();
                else
                    Scheduler.Schedule(TestCondition, _persistable);
            }

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                if (_serialized) return; _serialized = true;

                sd.Set(nameof(_predicate), _predicate);
                sd.Set(nameof(_callback), _callback);
                sd.Set(nameof(_persistable), _persistable);
            }

            private static UntilWrapper Deserialize(IReadOnlyDictionary<string, object> sd)
                => new UntilWrapper(
                    sd.Get<Func<bool>>(nameof(_predicate)),
                    sd.Get<Action>(nameof(_callback)),
                    sd.Get<bool>(nameof(_persistable))
                ) {_serialized = true};
        }
    }
}
