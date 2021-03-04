using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cleipnir.ExecutionEngine.Api;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.ObjectDB.TaskAndAwaitable.Awaitables;

namespace Cleipnir.ExecutionEngine.Providers
{
    public static class Sleep
    {
        public static CAwaitable Until(int delayMs, bool persistable = true)
            => Until(TimeSpan.FromMilliseconds(delayMs), persistable);

        public static CAwaitable Until(TimeSpan delay, bool persistable = true)
        {
            var awaitable = new CAwaitable();
            var scheduler = Engine.Current;
            if (!persistable)
                Task.Delay(delay).ContinueWith(_ => scheduler.Schedule(awaitable.SignalCompletion));
            else
                new PersistentSleeper(
                    DateTime.UtcNow + delay,
                    awaitable,
                    true
                ).Start();

            return awaitable;
        }

        public static void Until(int delayMs, Action callback, bool persistable)
            => Until(TimeSpan.FromMilliseconds(delayMs), callback, persistable);

        public static void Until(TimeSpan delay, Action callback, bool persistable)
        {
            var expires = DateTime.UtcNow + delay;
            var scheduler = Engine.Current;

            if (persistable)
                new CallAfterSleeper(expires, callback, true).Start();
            else
                Task.Delay(delay).ContinueWith(_ => scheduler.Schedule(callback));
        }

        private class PersistentSleeper : IPersistable
        {
            private readonly DateTime _expires;
            private readonly CAwaitable _awaitable;

            public PersistentSleeper(DateTime expires, CAwaitable awaitable, bool rootify)
            {
                _expires = expires;
                _awaitable = awaitable;

                if (rootify)
                    Roots.EntangleAnonymously(this);
            }

            public async void Start()
            {
                var expiresIn = _expires - DateTime.UtcNow;
                var scheduler = Engine.Current;
                if (expiresIn > TimeSpan.Zero)
                    await Task.Delay(expiresIn);

                _ = scheduler.Schedule(ExecuteCallback);
            }

            private void ExecuteCallback()
            {
                Roots.UntangleAnonymously(this);
                _awaitable.SignalCompletion();
            }

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(_expires), _expires);
                sd.Set(nameof(_awaitable), _awaitable);
            }

            private static PersistentSleeper Deserialize(IReadOnlyDictionary<string, object> sd)
            {
                var expires = (DateTime)sd[nameof(_expires)];
                var awaitable = (CAwaitable)sd[nameof(_awaitable)];

                var sleeper = new PersistentSleeper(expires, awaitable, false);

                if (!awaitable.Completed)
                    Engine.Current.Schedule(sleeper.Start);

                return sleeper;
            }
        }

        private class CallAfterSleeper : IPersistable
        {
            private readonly DateTime _expires;
            private readonly Action _callAfterSleep;

            private bool _executed;

            public CallAfterSleeper(DateTime expires, Action callAfterSleep, bool rootify)
            {
                _expires = expires;
                _callAfterSleep = callAfterSleep;

                if (rootify)
                    Roots.EntangleAnonymously(this);
            }

            public async void Start()
            {
                var expiresIn = _expires - DateTime.UtcNow;
                var scheduler = Engine.Current;

                if (expiresIn > TimeSpan.Zero)
                    await Task.Delay(expiresIn);

                _ = scheduler.Schedule(ExecuteCallback);
            }

            private void ExecuteCallback()
            {
                if (_executed) return;

                _executed = true;
                Roots.UntangleAnonymously(this);
                _callAfterSleep();
            }

            public void Serialize(StateMap sd, SerializationHelper helper)
            {
                sd.Set(nameof(_executed), _executed);
                sd.Set(nameof(_expires), _expires);
                sd.Set(nameof(_callAfterSleep), _callAfterSleep);
            }

            private static CallAfterSleeper Deserialize(IReadOnlyDictionary<string, object> sd)
            {
                var expires = (DateTime)sd[nameof(_expires)];
                var action = (Action)sd[nameof(_callAfterSleep)];
                var executed = (bool)sd[nameof(_executed)];

                var sleeper = new CallAfterSleeper(expires, action, false);

                if (!executed)
                    Engine.Current.Schedule(sleeper.Start);

                return sleeper;
            }
        }
    }
}
