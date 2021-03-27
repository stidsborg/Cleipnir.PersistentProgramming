using System;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Persistency.Version2
{
    public sealed class CircularDependencyException2 : Exception
    {
        public IEnumerable<long> CircularPath { get; }

        public IReadOnlyDictionary<long, IEnumerable<KeyValuePair<string, string>>> Maps;

        internal CircularDependencyException2(
            IEnumerable<long> circularPath, 
            IReadOnlyDictionary<long, IEnumerable<StorageEntry>> storageEntries)
        {
            CircularPath = circularPath;
            Maps = circularPath
                .Skip(1)
                .ToDictionary(
                    objectId => objectId,
                    objectId => storageEntries[objectId]
                        .Select(entry =>
                            new KeyValuePair<string, string>(
                                entry.Key,
                                entry.Reference.HasValue
                                    ? $"Reference -> {entry.Reference.Value}"
                                    : $"Value: {entry.Value}"
                            )
                        )
                        .ToList()
                        .AsEnumerable()
                );
        }
    }
}