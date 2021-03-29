using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Version2.Persistency.Serializers.DisplayClass
{
    public class DisplayClassSerializer : ISerializer2
    {
        public object Instance { get; }
        private bool _serialized;

        public DisplayClassSerializer(object instance, bool serialized)
        {
            Instance = instance;
            _serialized = serialized;
        }
        
        public void SerializeInto(Map2 m)
        {
            if (!_serialized) 
                m["¡Type"] = Instance.GetType().SimpleQualifiedName();
            _serialized = true;
            
            ReflectiveSerializer.Serialize(Instance, m);
        }
    }
}