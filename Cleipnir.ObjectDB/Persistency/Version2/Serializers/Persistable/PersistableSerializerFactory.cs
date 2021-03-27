using System;
using System.Reflection;

namespace Cleipnir.ObjectDB.Persistency.Version2.Serializers.Persistable
{
    public class PersistableSerializerFactory : ISerializerFactory
    {
        public bool CanSerialize(object instance) => instance is IPersistable2;
        
        public ISerializer2 CreateSerializer(object instance) 
            => new PersistableSerializer2((IPersistable2) instance, false);

        public ISerializer2 CreateSerializer(RMap rm, Ephemerals eps)
        {
            var type = Type.GetType(rm.Get<string>("¡Type"));
            var persistable = (IPersistable2) InvokeStaticDeserializeMethod(type, rm, eps);
            return new PersistableSerializer2(persistable, true);
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