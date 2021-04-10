using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers;

namespace Cleipnir.ObjectDB.Version2.PersistentDataStructures
{
    public class CQueue<T> : IPersistable2, IEnumerable<T>
    {
        private readonly ChangeTracker<T> _changeTracker;
        public int Count { get; private set; }
        
        public CQueue() => _changeTracker = new ChangeTracker<T>();

        private CQueue(ChangeTracker<T> changeTracker)
        {
            _changeTracker = changeTracker;
            Count = changeTracker.GetAllNodes().Count();
        }

        public void Enqueue(T value)
        {
            _changeTracker.Append(value);
            Count++;
        } 

        public T Dequeue()
        {
            if (Count == 0) throw new InvalidOperationException("Queue is empty");

            var head = _changeTracker.Head;
            head.Remove();

            Count--;
            
            return head.Value;
        }

        public void Serialize(Map2 m) => _changeTracker.Serialize(m);

        private static CQueue<T> Deserialize(RMap rm) => new CQueue<T>(ChangeTracker<T>.Deserialize(rm));

        public IEnumerator<T> GetEnumerator() => _changeTracker.GetAllNodes().Select(n => n.Value).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
