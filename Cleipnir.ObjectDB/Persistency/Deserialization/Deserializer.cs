using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Helpers.DataStructures;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.Persistency.Persistency;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Persistency.Deserialization
{
    internal static class Deserializer
    {
        public static State Load(IStorageEngine storageEngine, ISet<object> ephemeralInstances)
        {
            var valuesDictionaries = new DictionaryWithDefault<long, Dictionary<string, object>>(_ => new Dictionary<string, object>());

            //Load all entries from log file
            var storedState = storageEngine.Load();

            var serializers = new Serializers(new SerializerFactory());
            var stateMaps = new StateMaps(serializers);
            var entriesPerOwner = storedState.StorageEntries;
            //var entriesPerOwner = storedState.StorageEntries;

            var serializersTypeEntries = storedState.Serializers;

            var toDeserialize = new Queue<long>();
            var referenced = new DictionaryWithDefault<long, List<Reference>>(_ => new List<Reference>());

            ISerializer Deserialize(long id)
            {
                if (serializers.ContainsKey(id))
                    return serializers[id];

                var ownerEntries = entriesPerOwner.ContainsKey(id)
                    ? entriesPerOwner[id]
                    : Enumerable.Empty<StorageEntry>();

                var resolvedValues = valuesDictionaries[id];

                //deserialize non-referencing values
                var resolvableEntries = ownerEntries
                    .Where(e => !e.Reference.HasValue)
                    .ToList();

                foreach (var resolvableEntry in resolvableEntries)
                    resolvedValues[resolvableEntry.Key] = resolvableEntry.Value;

                var referencedEntries = ownerEntries.Where(e => e.Reference.HasValue);

                foreach (var referencedEntry in referencedEntries)
                {
                    // ReSharper disable once PossibleInvalidOperationException
                    var wp = Deserialize(referencedEntry.Reference.Value);
                    resolvedValues[referencedEntry.Key] = wp.Instance;
                }

                var serializer = (ISerializer) DeserializationMethodInvoker
                    .Invoke(
                        id,
                        serializersTypeEntries[id],
                        valuesDictionaries[id],
                        ephemeralInstances
                    );

                stateMaps[id] = new StateMap(serializers, resolvedValues);
                serializers.Add(serializer);

                if (serializer.Instance is Reference r && r.Id.HasValue)
                {
                    toDeserialize.Enqueue(r.Id.Value);
                    referenced[r.Id.Value].Add(r);
                }

                return serializer;
            }

            var roots = (Roots) Deserialize(0).Instance;
            ephemeralInstances.Add(roots);

            while (toDeserialize.Any())
                Deserialize(toDeserialize.Dequeue());

            foreach (var serializer in serializers)
                foreach (var reference in referenced[serializer.Id])
                    reference.SetSerializer(serializer);
            
            return new State(roots, stateMaps, serializers);
        }

        public record State(Roots Roots, StateMaps StateMaps, Serializers Serializers) { }
    }
}
