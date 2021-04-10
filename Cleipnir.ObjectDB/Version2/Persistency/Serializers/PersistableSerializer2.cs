using System;
using System.Reflection;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Version2.Persistency.Serializers
{
    public interface IPersistable2
    {
        void Serialize(Map2 m);
    }
    
    public class PersistableSerializer2 : ISerializer2, ISerializerFactory
    {
        public object Instance => Persistable;

        private IPersistable2 Persistable { get; init; }

        private bool _serialized;

        public void SerializeInto(Map2 m)
        {
            if (!_serialized)
            {
                m["¡Type"] = Persistable.GetType().SimpleQualifiedName();
                _serialized = true;
            }
            
            Persistable.Serialize(m);
        }

        public bool CanSerialize(object instance) => instance is IPersistable2;

        public ISerializer2 CreateSerializer(object instance)
            => new PersistableSerializer2 {Persistable = (IPersistable2) instance};

        public ISerializer2 CreateSerializer(RMap rm, Ephemerals eps)
        {
            var type = Type.GetType(rm.Get<string>("¡Type"));
            var persistable = (IPersistable2) InvokeStaticDeserializeMethod(type, rm, eps);
            return new PersistableSerializer2()
            {
                Persistable = persistable,
                _serialized = true
            };
        }
        
        private static object InvokeStaticDeserializeMethod(Type type, RMap rm, Ephemerals eps)
        {
            //todo create a method checking if the ipersistable has correct deserialize method
            var deSerializeMethod = type.GetMethod(
                "Deserialize",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (deSerializeMethod == null) //try with constructor instead
                throw new MissingMethodException($"{type} does not have static Deserialize-method"); //todo find specific exception

            var requiredParameters = deSerializeMethod.GetParameters();

            try
            {
                if (requiredParameters.Length == 0)
                    return deSerializeMethod.Invoke(null, null);
                if (requiredParameters.Length == 1)
                    return deSerializeMethod.Invoke(null, new object[] {rm});
                if (requiredParameters.Length == 2)
                    return deSerializeMethod.Invoke(null, new object[] {rm, eps});
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException != null)
                    throw e.InnerException;
                
                throw;
            }

            throw new MissingMethodException($"{type} does not have expected static Deserialize(RMap, Ephemerals)-method");
        }
    }
}