using System;

namespace Cleipnir.ObjectDB.Version2.Persistency.Serializers.Delegate
{
    internal class DelegateSerializerFactory : ISerializerFactory
    {
        public bool CanSerialize(object instance) => instance is System.Delegate;
        
        public ISerializer2 CreateSerializer(object instance)
            => new DelegateSerializer2((System.Delegate) instance, false);

        public ISerializer2 CreateSerializer(RMap rm, Ephemerals eps)
        {
            var delegateType = Type.GetType((string) rm["DelegateType"]);
            var target = rm["Target"];
            var methodName = (string) rm["MethodName"];

            if (target != null)
                return new DelegateSerializer2(System.Delegate.CreateDelegate(delegateType, target, methodName), true);

            var declaringType = Type.GetType((string) rm["DeclaringType"]);

            return new DelegateSerializer2(System.Delegate.CreateDelegate(delegateType, declaringType, methodName), true);
        }
    }
}