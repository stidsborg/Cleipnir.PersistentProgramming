using System;
using System.Runtime.CompilerServices;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers;
using Cleipnir.ObjectDB.Version2.TaskAndAwaitable.Awaitables;

namespace Cleipnir.ObjectDB.Version2.TaskAndAwaitable.StateMachine
{
    [AsyncMethodBuilder(typeof(CMethodBuilder2))]
    public class CTask2 : IPersistable2
    {
        public CTask2() => Awaitable = new CAwaitable2();

        private CTask2(CAwaitable2 awaitable) => Awaitable = awaitable;

        public CAwaitable2 GetAwaiter() => Awaitable.GetAwaiter();
        public CAwaitable2 Awaitable { get; }

        internal void SignalCompletion() => Awaitable.SignalCompletion();

        internal void SignalThrownException(Exception e) => Awaitable.SignalThrownException(e);
        
        public void Serialize(Map2 m)
            => m[nameof(Awaitable)] = Awaitable;

        private static CTask2 Deserialize(RMap rm) 
            => new CTask2((CAwaitable2) rm[nameof(Awaitable)]);
    }

    [AsyncMethodBuilder(typeof(CMethodBuilder2<>))]
    public class CTask2<T> : IPersistable2
    {
        public CTask2() => Awaitable = new CAwaitable2<T>();
        private CTask2(CAwaitable2<T> awaitable) => Awaitable = awaitable;

        internal void SignalCompletion(T result) => Awaitable.SignalCompletion(result);

        internal void SignalThrownException(Exception e) => Awaitable.SignalThrownException(e);

        public CAwaitable2<T> GetAwaiter() => Awaitable.GetAwaiter();
        public CAwaitable2<T> Awaitable { get; }

        public void Serialize(Map2 m) 
            => m[nameof(Awaitable)] = Awaitable;

        private static CTask2<T> Deserialize(RMap rm)
            => new CTask2<T>((CAwaitable2<T>) rm[nameof(Awaitable)]);
    }
}