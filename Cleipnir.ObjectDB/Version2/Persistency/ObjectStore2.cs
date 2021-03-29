using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Version2.Persistency
{
    public class ObjectStore2
    {
        private readonly Persister2 _persister;

        public IRoots2 Roots { get; }
        //public IStorageEngine StorageEngine { get; }

        //Used when data does not already exists
        private ObjectStore2(IStorageEngine storageEngine)
        {
            var serializerFactories = new SerializerFactories(SerializerFactories.DefaultFactories);
            var mapAndSerializers = new MapAndSerializers(serializerFactories);
            
            var roots = new Roots2();
            Roots = roots;
            
            mapAndSerializers.GetOrCreateSerializerFor(roots);
                
            _persister = new Persister2(storageEngine, mapAndSerializers);
        }

        //Used when data exists
        private ObjectStore2(IRoots2 roots, MapAndSerializers mapAndSerializers, IStorageEngine storageEngine)
        {
            Roots = roots;
            _persister = new Persister2(storageEngine, mapAndSerializers);
        }

        public void Persist() => _persister.DetectAndPersistChanges();

        public static ObjectStore2 Load(IStorageEngine storageEngine, params object[] ephemeralInstances)
        {
            var (roots, mapAndSerializers) = Deserializer2.Load(
                storageEngine,
                new Ephemerals(ephemeralInstances),
                new SerializerFactories(SerializerFactories.DefaultFactories)
            );

            return new ObjectStore2(
                roots,
                mapAndSerializers,
                storageEngine
            );
        }
        
        public static ObjectStore2 New(IStorageEngine storageEngine) 
            => new ObjectStore2(storageEngine);
    }
}