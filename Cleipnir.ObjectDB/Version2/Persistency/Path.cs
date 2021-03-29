using System;
using System.Collections.Generic;
using System.Linq;

namespace Cleipnir.ObjectDB.Version2.Persistency
{
    internal class Path
    {
        private readonly List<long> _path = new();

        public CircularityDetectedAndPath Push(long objectId)
        {
            var circularityDetected = _path.Contains(objectId);
            
            _path.Add(objectId);

            return new CircularityDetectedAndPath(
                circularityDetected,
                circularityDetected ? FindCircularPath(objectId) : Enumerable.Empty<long>()
            );
        }

        private IEnumerable<long> FindCircularPath(long insertedLast)
        {
            var from = _path.FindIndex(id => id == insertedLast);
            while (from < _path.Count)
                yield return _path[from++];
        }

        public void Pop()
        {
            if (_path.Count == 0)
                throw new InvalidOperationException("Cannot pop from empty Path");
            
            _path.RemoveAt(_path.Count - 1);
        }

        public override string ToString() 
            => string.Join("->", _path);
    }

    internal record CircularityDetectedAndPath(bool CircularityDetected, IEnumerable<long> Path);
}