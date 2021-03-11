using System;
using System.Reflection;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Persistency.Version2
{
    public class PersistableSerializer2 : ISerializer2, ISerializerFactory
    {
        public object Instance => _persistable;
        private readonly IPersistable2 _persistable;

        private bool _serialized;

        public PersistableSerializer2() { }

        public PersistableSerializer2(IPersistable2 persistable) => _persistable = persistable;

        public void SerializeInto(Map2 m)
        {
            if (!_serialized)
                m.Set("¡Type", _persistable.GetType().SimpleQualifiedName());
            _serialized = true;

            _persistable.Serialize(m);
        }

        // ** FACTORY METHODS ** //
        public ISerializer2 CreateSerializer(object instance) 
            => new PersistableSerializer2((IPersistable2) instance);

        public bool CanSerialize(object instance) => instance is IPersistable2;

        public ISerializer2 CreateFrom(RMap rm, Ephemerals eps)
        {
            var type = Type.GetType(rm.Get<string>("¡Type"));
            var persistable = (IPersistable2) InvokeStaticDeserializeMethod(type, rm, eps);
            return new PersistableSerializer2(persistable);
        }
        
        private static object InvokeStaticDeserializeMethod(Type type, RMap rm, Ephemerals eps)
        { //todo create a method checking if the ipersistable has correct deserialize method
            var deSerializeMethod = type.GetMethod(
                "Deserialize",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (deSerializeMethod == null) //try with constructor instead
                throw new MissingMethodException(
                    $"{type} does not have static Deserialize-method"); //todo find specific exception

            var requiredParameters = deSerializeMethod.GetParameters();

            if (requiredParameters.Length == 0)
                return deSerializeMethod.Invoke(null, null);
            if (requiredParameters.Length == 1)
                return deSerializeMethod.Invoke(null, new object[] {rm});
            if (requiredParameters.Length == 2)
                return deSerializeMethod.Invoke(null, new object[] {rm, eps});

            throw new MissingMethodException($"{type} does not have expected static Deserialize(RMap, Ephemerals)-method");
        }
    }
}