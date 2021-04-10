using System;
using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Version2.Persistency.Serializers
{
    public class DelegateSerializer2 : ISerializer2, ISerializerFactory
    {
        public object Instance => Delegate;

        private Delegate Delegate { get; init; }

        private bool _serialized;
        
        public void SerializeInto(Map2 m)
        {
            if (_serialized) return; _serialized = true;

            m["DelegateType"] = Delegate.GetType().SimpleQualifiedName();
            m["Target"] = Delegate.Target;
            m["MethodName"] = Delegate.Method.Name;
            m["DeclaringType"] = Delegate.Method.DeclaringType?.SimpleQualifiedName();
        }

        public bool CanSerialize(object instance) => instance is Delegate;

        public ISerializer2 CreateSerializer(object instance)
            => new DelegateSerializer2 { Delegate = (Delegate) instance };

        public ISerializer2 CreateSerializer(RMap rm, Ephemerals eps)
        {
            var delegateType = Type.GetType((string) rm["DelegateType"]);
            var target = rm["Target"];
            var methodName = (string) rm["MethodName"];

            if (target != null)
                return new DelegateSerializer2
                {
                    Delegate = Delegate.CreateDelegate(delegateType, target, methodName),
                    _serialized = true
                };

            var declaringType = Type.GetType((string) rm["DeclaringType"]);

            return new DelegateSerializer2
            {
                Delegate = Delegate.CreateDelegate(delegateType, declaringType, methodName),
                _serialized = true
            };
        }
    }
}