using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Helpers.DataStructures;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Version2.Persistency
{
    internal class Deserializer2
    {
        private readonly Queue<long> _deserializationQueue = new();

        private readonly DictionaryWithDefault<long, List<Action<ISerializer2>>> _resolvedListeners 
            = new(_ => new List<Action<ISerializer2>>());

        private readonly Dictionary<long, RMap> _rMaps = new();
        private readonly Dictionary<long, ISerializer2> _serializersDictionary = new ();

        private readonly Path _path = new();
        
        private readonly StoredState _storedState;
        
        private readonly SerializerFactories _serializerFactories;
        
        private readonly Ephemerals _emphereals;

        private Deserializer2(StoredState storedState, Ephemerals ephemerals, SerializerFactories serializerFactories)
        {
            _storedState = storedState;
            _emphereals = ephemerals;
            _serializerFactories = serializerFactories;
        }
        
        public static DeserializedState Load(IStorageEngine storageEngine, Ephemerals ephemerals, SerializerFactories serializerFactories)
        {
            var storedState = storageEngine.Load();

            var d = new Deserializer2(storedState, ephemerals, serializerFactories);
            return d.Deserialize();
        }

        public DeserializedState Deserialize()
        {
            var roots = (Roots2) Deserialize(Roots2.ObjectId).Instance;
            while (_deserializationQueue.Count > 0)
                _ = Deserialize(_deserializationQueue.Dequeue());

            foreach (var (key, listeners) in _resolvedListeners)
            {
                var serializer = _serializersDictionary[key];
                foreach (var listener in listeners)
                    listener(serializer);
            }

            var mapAndSerializers = new MapAndSerializers(_serializerFactories);
            foreach (var (objectId, rMap) in _rMaps)
            {
                var map = new Map2(mapAndSerializers, rMap.GetDeserializedValues());
                mapAndSerializers.Add(
                    objectId,
                    _serializersDictionary[objectId],
                    map
                );
            }

            return new DeserializedState(roots, mapAndSerializers);
        }

        internal ISerializer2 Deserialize(long objectId)
        {
            if (_serializersDictionary.ContainsKey(objectId))
                return _serializersDictionary[objectId];
            
            var (isCircular, path) = _path.Push(objectId);
            if (isCircular)
                throw new CircularDependencyException2(path, _storedState.StorageEntries);

            var storageEntries = _storedState.StorageEntries[objectId];

            var rMap = new RMap(this, storageEntries);
            _rMaps[objectId] = rMap;
            
            //find deserialization method on deserializer
            var serializerType = _storedState.Serializers[objectId];
            var factory = _serializerFactories.Find(serializerType);
            var serializer = factory.CreateSerializer(rMap, _emphereals);
            _serializersDictionary[objectId] = serializer;

            _path.Pop();
            
            return serializer;
        }

        public void RegisterCallbackWhenResolved(long objectId, Action<ISerializer2> callback)
        {
            _resolvedListeners[objectId].Add(callback);
            _deserializationQueue.Enqueue(objectId);
        }
    }

    internal record DeserializedState(
        Roots2 Roots,
        MapAndSerializers MapAndSerializers
    );
}