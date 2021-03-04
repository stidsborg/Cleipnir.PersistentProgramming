using System.Collections.Generic;
using System.Linq;
using Cleipnir.Helpers;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.Persistency.Persistency;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Persistency.Serialization
{
    internal class Persister
    {
        private readonly IStorageEngine _storageEngine;
        private readonly RootsInstance _roots;
        private readonly StateMaps _stateMaps;
        private readonly Serializers.Serializers _serializers;
        
        private long _maxObjectId;

        public Persister(
            IStorageEngine storageEngine,
            RootsInstance roots,
            Serializers.Serializers serializers, 
            StateMaps stateMaps)
        {
            _storageEngine = storageEngine;
            _roots = roots;
            _serializers = serializers;
            _stateMaps = stateMaps;
            _maxObjectId = serializers.NextObjectId - 1;
        }

        public void DetectSerializeAndPersistChanges(bool checkForCircularDependencies = false)
        {
            var detectedChanges = DetectChanges();
            if (detectedChanges.GarbageCollectableIds.Empty() &&
                detectedChanges.RemovedEntries.Empty() &&
                detectedChanges.NewEntries.Empty())
                return;

            if (checkForCircularDependencies)
            {
                var (isCircular, path) = CircularDependencyDetector.Check(root: _serializers[0], _stateMaps, _serializers);
                if (isCircular)
                    throw new CircularDependencyException(path);
            }

            _storageEngine.Persist(detectedChanges);
        }

        private DetectedChanges DetectChanges()
        {
            var changedEntries = new List<StorageEntry>();
            var removedEntries = new List<ObjectIdAndKey>();
            var newSerializerTypes = new List<ObjectIdAndType>();

            var readyToBeSerializeds = new Queue<ISerializer>(new[] { _serializers.AddAndWrapUp(_roots) });

            var serializationHelper = new SerializationHelper(_serializers);

            var serializedIds = new HashSet<long>();

            while (readyToBeSerializeds.Any())
            {
                var serializer = readyToBeSerializeds.Dequeue();
                var objectId = serializer.Id;
                if (serializedIds.Contains(objectId))
                    continue; //the object has already been serialized

                if (_maxObjectId < objectId) 
                    newSerializerTypes.Add(new ObjectIdAndType(objectId, serializer.GetType()));
                
                var stateMap = _stateMaps.Get(objectId);

                serializer.Serialize(stateMap, serializationHelper);

                //if the serializer is a Reference and it points to another object, then enqueue that objet's serializer  
                //please note that the Reference only holds a long. thus, without special handling we would not serialize
                //what it is pointing to 
                if (serializer.Instance is Reference r && r.Id.HasValue)
                    readyToBeSerializeds.Enqueue(_serializers[r.Id.Value]);

                //add referenced serializers to serialization queue
                var referencedSerializers = stateMap.GetReferencedSerializers();

                foreach (var referencedSerializer in referencedSerializers)
                    readyToBeSerializeds.Enqueue(referencedSerializer);

                //pull and add statemap changes to global list of changes
                var changes = stateMap
                    .PullChangedEntries()
                    .Select(change =>
                        change.HoldsSerializer
                            ? new StorageEntry(objectId, change.Key, change.Serializer.Id)
                            : new StorageEntry(objectId, change.Key, change.Value)
                    );

                changedEntries.AddRange(changes);
                removedEntries.AddRange(stateMap.PullRemovedKeys().Select(key => new ObjectIdAndKey(objectId, key)));

                //add just serialized entity to list of serialized ids in order to short circuit if/when reaching then same entity again
                serializedIds.Add(serializer.Id);
            }

            var garbageCollectables = _stateMaps.Ids.Where(id => !serializedIds.Contains(id)).ToList();
            foreach (var garbageCollectable in garbageCollectables)
            {
                _serializers.Remove(garbageCollectable);
                _stateMaps.Remove(garbageCollectable);
            }

            _maxObjectId = newSerializerTypes.Count == 0 
                ? _maxObjectId
                : newSerializerTypes.Select(r => r.ObjectId).Max();

            return new DetectedChanges(
                changedEntries, 
                removedEntries,
                newSerializerTypes,
                garbageCollectables
            );
        }
    }
}
