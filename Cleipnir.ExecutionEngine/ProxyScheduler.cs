using System;
using System.Collections.Generic;
using Cleipnir.ExecutionEngine.DataStructures;

namespace Cleipnir.ExecutionEngine
{
    internal class ProxyScheduler : IScheduler
    {
        private InternalScheduler _scheduler;
        public InternalScheduler Scheduler
        {
            get => _scheduler;
            set
            {
                foreach (var pendingToExecute in _pendingToExecutes)
                    value.Schedule(pendingToExecute.Action, pendingToExecute.IsPersistable);
                
                _pendingToExecutes.Clear();

                _scheduler = value;
            }
        }

        private readonly List<CAction> _pendingToExecutes = new List<CAction>();

        public void Start() => Scheduler.Start();
        public void Stop() => Scheduler.Stop();
        
        public void Schedule(Action toExecute, bool persistent)
        {
            if (_scheduler == null)
                _pendingToExecutes.Add(new CAction(toExecute, persistent));
            else
                _scheduler.Schedule(toExecute, persistent);
        }

        public void FireAndForget(Action toExecute)
        {
            if (Scheduler != null)
                Scheduler.FireAndForget(toExecute);
            else
                Schedule(toExecute, false);
        }

        public void Dispose() => Scheduler.Dispose();
    }
}