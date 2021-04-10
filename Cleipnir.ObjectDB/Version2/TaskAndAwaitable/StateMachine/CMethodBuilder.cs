using System;
using System.Runtime.CompilerServices;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Version2.TaskAndAwaitable.StateMachine
{
    public class CMethodBuilder2 : IPersistable2
    {
        public static CMethodBuilder2 Create() => new CMethodBuilder2();
        
        public CTask2 Task { get; private set;  } 
        private IAsyncStateMachine StateMachine { get; set; }

        public CMethodBuilder2() => Task = new CTask2();

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            StateMachine = stateMachine; //box state machine straight away
            StateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) => StateMachine = stateMachine;

        public void SetException(Exception exception) => Task.SignalThrownException(exception);

        public void SetResult() => Task.SignalCompletion();

        private void MoveNext() => StateMachine.MoveNext();

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine  
            => awaiter.OnCompleted(MoveNext);

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => awaiter.OnCompleted(MoveNext);

        public void Serialize(Map2 m)
        {
            ReflectiveSerializer.Serialize(StateMachine, m);

            m["¡StmType"] = StateMachine.GetType().SimpleQualifiedName();
            m["¡Task"] = Task;
        }

        internal static CMethodBuilder2 Deserialize(RMap rm)
        {
            var stmType = Type.GetType(rm["¡StmType"].ToString());
            var stmInstance = (IAsyncStateMachine) Activator.CreateInstance(stmType);
            
            ReflectiveSerializer.Deserialize(rm, stmInstance);

            var builder = new CMethodBuilder2();

            rm.WhenResolved<CTask2>("¡Task", t => builder.Task = t);
            builder.StateMachine = stmInstance;
            return builder;
        }
    }

    public class CMethodBuilder2<T> : IPersistable2
    {
        public static CMethodBuilder2<T> Create() => new CMethodBuilder2<T>();

        public CMethodBuilder2() => Task = new CTask2<T>();

        private IAsyncStateMachine StateMachine { get; set; }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
            where TStateMachine : IAsyncStateMachine
        {
            StateMachine = stateMachine; //box immediately
            StateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine) => StateMachine = stateMachine; 

        public void SetException(Exception exception) => Task.SignalThrownException(exception);

        public void SetResult(T result) => Task.SignalCompletion(result);
        public CTask2<T> Task { get; private set; }

        private void MoveNext() => StateMachine.MoveNext();

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine 
            => awaiter.OnCompleted(MoveNext);

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
            => awaiter.OnCompleted(MoveNext);

        public void Serialize(Map2 m)
        {
            ReflectiveSerializer.Serialize(StateMachine, m);

            m["¡StmType"] = StateMachine.GetType().SimpleQualifiedName();
            m["¡Task"] = Task;
        }

        private static CMethodBuilder2<T> Deserialize(RMap rm)
        {
            var stmType = Type.GetType(rm["¡StmType"].ToString());
            var stmInstance = (IAsyncStateMachine) Activator.CreateInstance(stmType);
            
            ReflectiveSerializer.Deserialize(rm, stmInstance);

            var builder = new CMethodBuilder2<T>();
            
            rm.WhenResolved<CTask2<T>>("¡Task", t => builder.Task = t);
            builder.StateMachine = stmInstance;
            
            return builder;
        }
    }
}