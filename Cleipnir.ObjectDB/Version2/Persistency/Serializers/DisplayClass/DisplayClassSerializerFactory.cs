using System;

namespace Cleipnir.ObjectDB.Version2.Persistency.Serializers.DisplayClass
{
    public class DisplayClassSerializerFactory : ISerializerFactory
    {
        public bool CanSerialize(object instance)
            => IsDisplayClass(instance.GetType());

        public ISerializer2 CreateSerializer(object instance)
            => new DisplayClassSerializer(instance, false);

        public ISerializer2 CreateSerializer(RMap rm, Ephemerals eps)
        {
            var type = Type.GetType(rm["¡Type"].ToString());
            var instance = Activator.CreateInstance(type);
            ReflectiveSerializer.Deserialize(rm, instance);

            return new DisplayClassSerializer(instance, true);
        }
        
        private static bool IsDisplayClass(Type t)
            => t.Name.Contains("<");
    }
}