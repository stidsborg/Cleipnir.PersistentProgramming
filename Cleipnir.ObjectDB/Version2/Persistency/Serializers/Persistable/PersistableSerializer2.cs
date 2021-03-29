using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Version2.Persistency.Serializers.Persistable
{
    public class PersistableSerializer2 : ISerializer2
    {
        public object Instance => _persistable;
        private readonly IPersistable2 _persistable;

        private bool _serialized;

        public PersistableSerializer2(IPersistable2 persistable, bool serialized)
        {
            _persistable = persistable;
            _serialized = serialized;
        } 

        public void SerializeInto(Map2 m)
        {
            if (!_serialized)
            {
                m["¡Type"] = _persistable.GetType().SimpleQualifiedName();
                _serialized = true;
            }
            
            _persistable.Serialize(m);
        }
    }
}