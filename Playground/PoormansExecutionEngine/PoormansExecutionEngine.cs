using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.PersistentDataStructures;
using Cleipnir.StorageEngine;

namespace Playground.PoormansExecutionEngine
{
    public class PoormansExecutionEngine
    {
        private CQueue<Action> _workQueue;
        private readonly List<Action> _tempWorkQueue = new();
        private volatile bool _stop;

        private readonly object _sync = new();
        private ObjectStore _objectStore;

        public void Start(IStorageEngine storageEngine, bool existing)
        {
            if (existing)
            {
                _objectStore = ObjectStore.Load(storageEngine, this);
                _workQueue = _objectStore.Resolve<CQueue<Action>>();
            }
            else
            {
                _objectStore = ObjectStore.New(storageEngine);
                _workQueue = new();
                _objectStore.Entangle(_workQueue);
            }

            Task.Run(ExecuteEventLoop);
        }

        public void Stop()
        {
            _stop = true;
        }

        public void Entangle(object root)
        {
            lock (_sync) 
                _objectStore.Roots.Entangle(root);
        }

        public void Untangle(object root)
        {
            lock (_sync)
                _objectStore.Roots.Untangle(root);
        }

        public void Schedule(Action workItem)
        {
            lock (_sync)
            {
                if (_workQueue == null)
                    _tempWorkQueue.Add(workItem);
                else 
                    _workQueue.Enqueue(workItem);
            }
                
        }

        private void ExecuteEventLoop()
        {
            lock (_sync)
                _tempWorkQueue.ForEach(_workQueue.Enqueue);
            
            while (!_stop)
            {
                Action workItem;
                lock (_sync)
                    if (_workQueue.Count == 0)
                        workItem = null;
                    else
                        workItem = _workQueue.Dequeue();

                if (workItem == null) 
                    Thread.Sleep(100);
                else
                    workItem();
                
                _objectStore.Persist();
            }
        }
    }
}