using System;
using System.Collections.Generic;

namespace Cleipnir.StorageEngine
{
    public interface IStorageEngine : IDisposable
    {
        void Persist(DetectedChanges detectedChanges);
        StoredState Load();
    }

    public record StoredState(
        IReadOnlyDictionary<long, Type> Serializers,
        IReadOnlyDictionary<long, IEnumerable<StorageEntry>> StorageEntries
    );
}
