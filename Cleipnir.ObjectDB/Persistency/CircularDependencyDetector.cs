using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.Persistency.Persistency;

namespace Cleipnir.ObjectDB.Persistency
{
    internal class CircularDependencyDetector
    {
        public CircularPath Check(ISerializer root, StateMaps stateMaps, Serializers serializers)
        {
            var visited = new HashSet<ISerializer>();
            var stack = new Stack<SerializerAndPath>();
            var refsQueue = new Queue<ISerializer>();
            
            refsQueue.Enqueue(root);
            
            while (refsQueue.Count > 0)
            {
                stack.Push(
                    new SerializerAndPath(refsQueue.Dequeue(),
                        ImmutableList<ISerializer>.Empty)
                );

                while (stack.Count > 0)
                {
                    var (curr, path) = stack.Pop();

                    if (visited.Contains(curr))
                        continue;
                    visited.Add(curr);

                    path = path.Add(curr);

                    if (curr is ReferenceSerializer)
                    {
                        var reference = (Reference) curr.Instance;
                        if (reference.Id.HasValue)
                            refsQueue.Enqueue(serializers[reference.Id.Value]);
                        continue;
                    }
                    
                    var referencedSerializers = stateMaps
                        .Get(curr.Id)
                        .GetReferencedSerializers();
                    
                    foreach (var referencedSerializer in referencedSerializers)
                    {
                        if (path.Contains(referencedSerializer))
                        {
                            var circularPath = path
                                .Append(referencedSerializer)
                                .Aggregate(
                                    ImmutableList<object>.Empty,
                                    (l, s) => l.Any() || s == referencedSerializer ? l.Add(s.Instance) : l
                                );
                            return new CircularPath(true, circularPath);
                        }
                        
                        stack.Push(new SerializerAndPath(referencedSerializer, path));
                    }
                }
            }

            return new CircularPath(false, Enumerable.Empty<object>());
        }

        private record SerializerAndPath(ISerializer Serializer, ImmutableList<ISerializer> Path);

        internal record CircularPath(bool IsCircular, IEnumerable<object> Path)
        {
            public override string ToString() => string.Join("->", Path.Select(o => o.ToString()));
        }
    }
}