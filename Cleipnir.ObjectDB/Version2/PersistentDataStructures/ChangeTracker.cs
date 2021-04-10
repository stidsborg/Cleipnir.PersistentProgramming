using System;
using System.Collections.Generic;
using Cleipnir.Helpers;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers;
using static System.Math;

namespace Cleipnir.ObjectDB.Version2.PersistentDataStructures
{
    internal class ChangeTracker<T> : IPersistable2
    {
        public Node Head { get; private set; }
        public Node Tail { get; private set; }
        private int NextId { get; set; }
        
        private ISet<Node> ChangedNodes { get; } = new HashSet<Node>(ReferenceEqualityComparer.Instance);
        private ISet<Node> RemovedNodes { get; } = new HashSet<Node>(ReferenceEqualityComparer.Instance);

        public Node Append(T value)
        {
            if (Tail == null && Head == null)
            {
                var newNode = new Node(NextId++, this) { Value = value };
                ChangedNodes.Add(newNode);
                
                return Head = Tail = newNode;
            }

            return Tail = Tail.AddAfter(value);
        }

        public Node Prepend(T value)
        {
            if (Tail == null && Head == null)
            {
                var newNode = new Node(NextId++, this) { Value = value };
                ChangedNodes.Add(newNode);

                return Head = Tail = newNode;
            }
            
            return Head = Head.AddBefore(value);
        }

        public IEnumerable<Node> GetAllNodes()
        {
            var curr = Head;
            while (curr != null)
            {
                yield return curr;
                curr = curr.Next;
            }
        }

        public void Serialize(Map2 m)
        {
            if (ChangedNodes.Empty() && RemovedNodes.Empty())
                return;

            m["¡RootId"] = Head?.Id;
            
            foreach (var changedNode in ChangedNodes)
                changedNode.Serialize(m);

            foreach (var removedNode in RemovedNodes)
                removedNode.Remove(m);
            
            ChangedNodes.Clear();
            RemovedNodes.Clear();
        }

        public static ChangeTracker<T> Deserialize(RMap rm)
        {
            if (!rm.ContainsKey("¡RootId") || rm["¡RootId"] == null)
                return new ChangeTracker<T>();

            var changeTracker = new ChangeTracker<T>();

            var rootId = rm.Get<int>("¡RootId");
            var maxId = rootId;
            
            var (currNode, nextId) = Node.Deserialize(rm, rootId, changeTracker);
            changeTracker.Head = currNode;
            changeTracker.Tail = currNode;
            
            while (nextId != null)
            {
                maxId = Max(nextId.Value, maxId);
                var (nextNode, id) = Node.Deserialize(rm, nextId.Value, changeTracker);
                nextId = id;
                
                currNode.Next = nextNode;
                nextNode.Prev = currNode;
                currNode = nextNode;
                changeTracker.Tail = currNode;
            }

            changeTracker.NextId = maxId + 1;
            return changeTracker;
        }

        public class Node
        {
            public int Id { get; private set; }

            private T _value;
            public T Value
            {
                get => _value;
                set
                {
                    _value = value;
                    ChangeTracker.ChangedNodes.Add(this);
                }
            }

            public Node Next { get; set; }
            public Node Prev { get; set; }

            private ChangeTracker<T> ChangeTracker { get; }

            public Node(int id, ChangeTracker<T> changeTracker)
            {
                Id = id;
                ChangeTracker = changeTracker;  
            } 

            public void Remove()
            {
                ChangeTracker.RemovedNodes.Add(this);

                if (ChangeTracker.Head == this)
                    ChangeTracker.Head = Next;
                if (ChangeTracker.Tail == this)
                    ChangeTracker.Tail = Prev;

                if (Prev != null)
                {
                    Prev.Next = Next;
                    ChangeTracker.ChangedNodes.Add(Prev);
                }
                
                if (Next != null)
                    Next.Prev = Prev;
            }

            public Node AddAfter(T value)
            {
                var newNode = new Node(ChangeTracker.NextId++, ChangeTracker)
                {
                    Value = value,
                    Prev = this,
                    Next = this.Next
                };

                ChangeTracker.ChangedNodes.Add(newNode);
                ChangeTracker.ChangedNodes.Add(this);
                
                this.Next = newNode;

                if (newNode.Next != null)
                    newNode.Next.Prev = newNode;
                
                return newNode;
            }

            public Node AddBefore(T value)
            {
                var newNode = new Node(ChangeTracker.NextId++, ChangeTracker)
                {
                    Value = value,
                    Prev = this.Prev,
                    Next = this
                };

                this.Prev = newNode;

                if (newNode.Prev != null)
                {
                    newNode.Prev.Next = newNode;
                    ChangeTracker.ChangedNodes.Add(newNode.Prev);
                }

                return newNode;
            }

            public void Serialize(Map2 m)
            {
                m[$"¡{Id}_Next"] = Next?.Id;
                m[$"¡{Id}_Value"] = Value;
            }

            public void Remove(Map2 m)
            {
                m.Remove($"¡{Id}_Next");
                m.Remove($"¡{Id}_Value");
            }

            internal static NodeAndNextNodeId Deserialize(RMap rm, int id, ChangeTracker<T> changeTracker)
            {
                var node = new Node(id, changeTracker);
                rm.WhenResolved<T>(
                    $"¡{id}_Value",
                    t => node._value = t
                );
                var nextId = rm[$"¡{id}_Next"].CastTo<int?>();
                return new NodeAndNextNodeId(node, nextId);
            }

            internal record NodeAndNextNodeId(Node Node, int? NextId);

            internal static void WhenValueResolved(RMap rm, Node n, Action a) 
                => rm.WhenResolved<T>($"¡{n.Id}_Value", _ => a());
        }
    }
}