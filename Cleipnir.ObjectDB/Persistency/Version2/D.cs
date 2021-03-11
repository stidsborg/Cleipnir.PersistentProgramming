using System;
using System.Collections.Generic;
using Cleipnir.ObjectDB.Helpers.DataStructures;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Persistency.Version2
{
    internal class D
    {
        private readonly Queue<long> _serializationQueue = new Queue<long>();

        private readonly DictionaryWithDefault<long, List<Action<ISerializer2>>> _resolvedListeners 
            = new(_ => new List<Action<ISerializer2>>());

        private readonly Dictionary<long, RMap> _rMaps = new();
        private readonly Dictionary<long, ISerializer2> _serializersDictionary = new ();
        
        private readonly StoredState _storedState;
        
        private readonly SerializerFactories _serializerFactories;
        
        private readonly Ephemerals _emphereals;

        public D(StoredState storedState, Ephemerals emphereals, SerializerFactories serializerFactories)
        {
            _storedState = storedState;
            _emphereals = emphereals;
            _serializerFactories = serializerFactories;
        }
        
        public static Deserializer.State Load(IStorageEngine storageEngine, Ephemerals ephemerals, SerializerFactories serializerFactories)
        {
            var storedState = storageEngine.Load();

            var d = new D(storedState, ephemerals, serializerFactories);
            d.Deserialize();

            return null;
        }

        public object Deserialize()
        {
            var root = Deserialize(Roots.ObjectId).Instance;
            while (_serializationQueue.Count > 0)
                _ = Deserialize(_serializationQueue.Dequeue());
            
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
            
            return root;
        } 
        public ISerializer2 Deserialize(long objectId)
        {
            if (_serializersDictionary.ContainsKey(objectId))
                return _serializersDictionary[objectId];

            var storageEntries = _storedState.StorageEntries[objectId];

            var rMap = new RMap(this, storageEntries);
            _rMaps[objectId] = rMap;
            
            //find deserialization method on deserializer
            var serializerType = _storedState.Serializers[objectId];
            var factory = _serializerFactories.Find(serializerType);
            var serializer = factory.CreateFrom(rMap, _emphereals);
            _serializersDictionary[objectId] = serializer;

            return serializer;
        }

        public void RegisterCallbackWhenResolved(long objectId, Action<ISerializer2> callback)
        {
            _resolvedListeners[objectId].Add(callback);
            _serializationQueue.Enqueue(objectId);
        }
    }
}