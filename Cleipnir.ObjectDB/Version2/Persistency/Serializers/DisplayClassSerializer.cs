using System;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Version2.Persistency.Serializers
{
    public class DisplayClassSerializer : ISerializer2, ISerializerFactory
    {
        public object Instance { get; init; }
        private bool _serialized;

        public void SerializeInto(Map2 m)
        {
            if (!_serialized)
            {
                m["¡Type"] = Instance.GetType().SimpleQualifiedName();
                _serialized = true;
            }

            ReflectiveSerializer.Serialize(Instance, m);
        }

        public bool CanSerialize(object instance)
        {
            var isDisplayClass = instance.GetType().Name.Contains("<");
            return isDisplayClass;
        }

        public ISerializer2 CreateSerializer(object instance) => new DisplayClassSerializer {Instance = instance};

        public ISerializer2 CreateSerializer(RMap rm, Ephemerals eps)
        {
            var type = Type.GetType(rm["¡Type"].ToString());
            var instance = Activator.CreateInstance(type);
            ReflectiveSerializer.Deserialize(rm, instance);

            return new DisplayClassSerializer
            {
                Instance = instance,
                _serialized = true
            };
        }
    }
}