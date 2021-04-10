using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers;

namespace Cleipnir.ObjectDB.Version2.TaskAndAwaitable.Awaitables
{
    public class CAwaitable2 : IPersistable2, INotifyCompletion
    {
        private readonly List<Action> _awaiters;
        private Exception _thrownException;

        public bool IsCompleted => IsSuccessfullyCompleted || IsExceptionThrown;
        public bool IsSuccessfullyCompleted { get; private set; }
        public bool IsExceptionThrown => _thrownException != null;

        public CAwaitable2() => _awaiters = new List<Action>(1);

        private CAwaitable2(List<Action> awaiters, bool successfullyCompleted, Exception thrownException)
        {
            _awaiters = awaiters;
            _thrownException = thrownException;
            IsSuccessfullyCompleted = successfullyCompleted;
        }
        
        public void GetResult()
        {
            if (!IsCompleted)
                throw new InvalidOperationException("No result has been set yet. Please only call after OnCompleted callback");
            if (IsExceptionThrown)
                throw _thrownException;
        }

        public void OnCompleted(Action callback)
        {
            if (IsCompleted)
                callback();
            else
                _awaiters.Add(callback);
        }

        public void SignalCompletion()
        {
            if (IsCompleted)
                throw new InvalidOperationException("Completion or Exception has already been set for the awaitable");

            IsSuccessfullyCompleted = true;

            foreach (var awaiter in _awaiters)
                awaiter();

            _awaiters.Clear();
        }

        public void SignalThrownException(Exception e)
        {
            if (IsCompleted)
                throw new InvalidOperationException("Completion or Exception has already been set for the awaitable");

            _thrownException = e;

            foreach (var awaiter in _awaiters)
                awaiter();

            _awaiters.Clear();
        }

        public CAwaitable2 GetAwaiter() => this;

        public void Serialize(Map2 m)
        {
            m[nameof(IsSuccessfullyCompleted)] = IsSuccessfullyCompleted;
            m[nameof(_thrownException)] = _thrownException;
            ListSerializer.Serialize(_awaiters, m);
        }

        private static CAwaitable2 Deserialize(RMap rm)
        {
            var isSuccessfullyCompleted = rm.Get<bool>(nameof(IsSuccessfullyCompleted));
            var exception = rm.Get<Exception>(nameof(_thrownException));
            var awaiters = ListSerializer.Deserialize<Action>(rm);

            return new CAwaitable2(awaiters, isSuccessfullyCompleted, exception);
        }
    }

    public class CAwaitable2<T> : IPersistable2, INotifyCompletion
    {
        private readonly List<Action> _awaiters;

        private T _result;
        private Exception _thrownException;
        
        public bool IsCompleted => IsSuccessfullyCompleted || IsExceptionThrown;
        public bool IsSuccessfullyCompleted { get; private set; }
        public bool IsExceptionThrown => _thrownException != null;

        public CAwaitable2() => _awaiters = new List<Action>(1);

        private CAwaitable2(bool isSuccessfullyCompleted, Exception thrownException, T result, List<Action> awaiters) 
        {
            IsSuccessfullyCompleted = isSuccessfullyCompleted;
            _thrownException = thrownException;
            _result = result;
            _awaiters = awaiters;
        }

        public void SignalCompletion(T result)
        {
            if (IsCompleted)
                throw new InvalidOperationException("Completion or Exception has already been set for the awaitable");

            _result = result;
            IsSuccessfullyCompleted = true;

            foreach (var awaiter in _awaiters)
                awaiter();

            _awaiters.Clear();
        }

        public void SignalThrownException(Exception e)
        {
            if (IsCompleted)
                throw new InvalidOperationException("Completion or Exception has already been set for the awaitable");

            _thrownException = e;

            foreach (var awaiter in _awaiters)
                awaiter();

            _awaiters.Clear();
        }

        public void Serialize(Map2 m)
        {
            m[nameof(IsSuccessfullyCompleted)] = IsSuccessfullyCompleted;
            m[nameof(_thrownException)] = _thrownException;
            m[nameof(_result)] = _result;
            ListSerializer.Serialize(_awaiters, m);
        }

        internal static CAwaitable2<T> Deserialize(RMap rm)
        {
            var isSuccessfullyCompleted = rm.Get<bool>(nameof(IsSuccessfullyCompleted));
            var exception = rm.Get<Exception>(nameof(_thrownException));
            var result = rm.Get<T>(nameof(_result));
            var awaiters = ListSerializer.Deserialize<Action>(rm);
            
            return new CAwaitable2<T>(isSuccessfullyCompleted, exception, result, awaiters);
        }

        public CAwaitable2<T> GetAwaiter() => this;

        public void OnCompleted(Action continuation)
        {
            if (IsCompleted)
                continuation();
            else
                _awaiters.Add(continuation);
        }

        public T GetResult()
        {
            if (IsSuccessfullyCompleted)
                return _result;
            if (IsExceptionThrown)
                throw _thrownException;

            throw new InvalidOperationException("No result has been set yet. Please only call after OnCompleted callback");
        }
    }
}