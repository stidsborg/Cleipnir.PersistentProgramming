using Cleipnir.StorageEngine;

namespace Cleipnir.ObjectDB.Persistency.Version2.Serializers.Delegate
{
    public class DelegateSerializer2 : ISerializer2
    {
        public object Instance => _delegate;

        private readonly System.Delegate _delegate;

        private bool _serialized;

        public DelegateSerializer2(System.Delegate @delegate, bool serialized)
        {
            _delegate = @delegate;
            _serialized = serialized;
        }

        public void SerializeInto(Map2 m)
        {
            if (_serialized) return; _serialized = true;

            m["DelegateType"] = _delegate.GetType().SimpleQualifiedName();
            m["Target"] = _delegate.Target;
            m["MethodName"] = _delegate.Method.Name;
            m["DeclaringType"] = _delegate.Method.DeclaringType?.SimpleQualifiedName();
        }
    }
}