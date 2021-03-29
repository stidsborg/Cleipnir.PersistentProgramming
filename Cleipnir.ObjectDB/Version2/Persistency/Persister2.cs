using System.Collections.Generic;
using System.Linq;
using Cleipnir.Helpers;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Version2.Persistency
{
    internal class Persister2
    {
        private readonly IStorageEngine _storageEngine;
        private readonly MapAndSerializers _mapAndSerializers;
        private ISet<long> _reachableObjectIds = new HashSet<long>();

        public Persister2(IStorageEngine storageEngine, MapAndSerializers mapAndSerializers)
        {
            _storageEngine = storageEngine;
            _mapAndSerializers = mapAndSerializers;
        }

        public void DetectAndPersistChanges() 
            => _storageEngine.Persist(DetectChanges());

        private DetectedChanges DetectChanges()
        {
            var execution = new Execution(_mapAndSerializers, _reachableObjectIds);
            var (detectedChanges, reachableObjectIds) = execution.Perform();

            _reachableObjectIds = reachableObjectIds;
            return detectedChanges;
        }
        
        private class Execution
        {
            private readonly MapAndSerializers _mapAndSerializers;
            private readonly IEnumerable<long> _activeObjectIds;

            private readonly Stack<long> _serializationQueue = new();

            private readonly ISet<long> _visitedObjectIds = new HashSet<long>();
            private readonly List<StorageEntry> _changes = new();
            private readonly List<ObjectIdAndKey> _removed = new();

            public Execution(MapAndSerializers mapAndSerializers, IEnumerable<long> activeObjectIds)
            {
                _mapAndSerializers = mapAndSerializers;
                _activeObjectIds = activeObjectIds;
            }

            public DetectedChangesAndReachableIds Perform()
            {
                // serialize the roots instance
                _serializationQueue.Push(Roots2.ObjectId);

                // serialize all reachable instances
                while (_serializationQueue.Count > 0)
                    Serialize(_serializationQueue.Pop());

                // pull serializer factories for new types
                var newSerializerFactoryTypes = _mapAndSerializers.PullNewSerializerFactoryTypes();
                
                //find garbage collectable instances
                var garbageCollectableIds = new List<long>();
                foreach (var objectId in _activeObjectIds)
                    if (!_visitedObjectIds.Contains(objectId))
                        garbageCollectableIds.Add(objectId);
                
                return new DetectedChangesAndReachableIds(
                    new DetectedChanges(
                        _changes,
                        _removed,
                        newSerializerFactoryTypes,
                        garbageCollectableIds
                    ),
                    _visitedObjectIds);
            }

            private void Serialize(long objectId)
            {
                if (_visitedObjectIds.Contains(objectId))
                    return;
                _visitedObjectIds.Add(objectId);

                var (map, serializer) = _mapAndSerializers[objectId]; 
                serializer.SerializeInto(map);

                map
                    .GetReferencedSerializers()
                    .Select(r => r.ObjectId)
                    .ForEach(_serializationQueue.Push);
                
                var instanceChanges = map
                    .PullChangedEntries()
                    .Select(c =>
                        c.HoldsSerializer
                            ? new StorageEntry(objectId, c.Key, c.SerializerAndObjectId.ObjectId)
                            : new StorageEntry(objectId, c.Key, c.Value)
                    ).ToList(); //TODO remove ToList invocation
            
                _changes.AddRange(instanceChanges);

                var removedInstances = map
                    .PullRemovedKeys()
                    .Select(k => new ObjectIdAndKey(objectId, k))
                    .ToList(); //TODO remove ToList invocation
               
                _removed.AddRange(removedInstances);
            }

            public record DetectedChangesAndReachableIds(DetectedChanges DetectedChanges, ISet<long> ReachableObjectIds);
        }
    }
}
