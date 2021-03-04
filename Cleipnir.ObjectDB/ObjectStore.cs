using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB
{
    public class ObjectStore
    {
        private readonly Persister _persister;

        public RootsInstance Roots { get; }
        public IStorageEngine StorageEngine { get; }

        //Used when data does not already exists
        private ObjectStore(IStorageEngine storageEngine)
        {
            StorageEngine = storageEngine;
            var roots = new RootsInstance();
            var serializers = new Serializers(new SerializerFactory());
            var stateMaps = new StateMaps(serializers);

            Roots = roots;

            _persister = new Persister(StorageEngine, Roots, serializers, stateMaps);
        }

        //Used when data exists
        private ObjectStore(
            RootsInstance roots, 
            StateMaps stateMaps, Serializers serializers,
            IStorageEngine storageEngine)
        {
            Roots = roots;
            StorageEngine = storageEngine;

            _persister = new Persister(StorageEngine, Roots, serializers, stateMaps);
        }

        public void Attach(object toAttach) => Roots.Entangle(toAttach);

        public T Resolve<T>() => Roots.Resolve<T>();
        public IEnumerable<T> ResolveAll<T>() => Roots.ResolveAll<T>();

        public void Persist(bool checkForCircularDependencies = false) => _persister.DetectSerializeAndPersistChanges(checkForCircularDependencies);

        public static ObjectStore Load(IStorageEngine storageEngine, params object[] ephemeralInstances)
        {
            var (roots, stateMaps, serializers) = Deserializer.Load(storageEngine, new HashSet<object>(ephemeralInstances));
            return new ObjectStore(roots, stateMaps, serializers, storageEngine);
        }
        
        public static ObjectStore New(IStorageEngine storageEngine) 
            => new ObjectStore(storageEngine);
    }
}
