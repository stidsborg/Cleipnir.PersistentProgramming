using System;
using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers;

namespace Cleipnir.ObjectDB.Version2.PersistentDataStructures
{
    public static class CActionWithSpecifiedParameters
    {
        public static CActionWithSpecifiedParameters<T> Create<T>(T parameter, Action<T> method) 
            => new CActionWithSpecifiedParameters<T>(method, parameter);

        public static CActionWithSpecifiedParameters<T1, T2> Create<T1, T2>(T1 parameter1, T2 parameter2, Action<T1, T2> method)
            => new CActionWithSpecifiedParameters<T1, T2>(method, parameter1, parameter2);
    }

    public class CActionWithSpecifiedParameters<T> : IPersistable2
    {
        private readonly T _parameter1;
        private readonly Action<T> _method;

        private bool _serialized;

        public CActionWithSpecifiedParameters(Action<T> method, T parameter1)
        {
            _method = method;
            _parameter1 = parameter1;
        }

        public void Invoke() => _method(_parameter1);

        public Action Action => Invoke;

        public void Serialize(Map2 m)
        {
            if (_serialized) return; _serialized = true;

            m[nameof(_parameter1)] = _parameter1;
            m[nameof(_method)] = _method;
        }

        private static CActionWithSpecifiedParameters<T> Deserialize(RMap rm)
        {
            return new CActionWithSpecifiedParameters<T>(
                rm.Get<Action<T>>(nameof(_method)),
                rm.Get<T>(nameof(_parameter1))
            ) { _serialized = true };
        }

        public static implicit operator Action(CActionWithSpecifiedParameters<T> a) => a.Action;

    }

    public class CActionWithSpecifiedParameters<T1, T2> : IPersistable2
    {
        private readonly T1 _parameter1;
        private readonly T2 _parameter2;
        private readonly Action<T1, T2> _method;

        private bool _serialized;

        public CActionWithSpecifiedParameters(Action<T1, T2> method, T1 parameter1, T2 parameter2)
        {
            _method = method;
            _parameter1 = parameter1;
            _parameter2 = parameter2;
        }

        public void Invoke() => _method(_parameter1, _parameter2);

        public Action Action => Invoke;

        public void Serialize(Map2 m)
        {
            if (_serialized) return; _serialized = true;

            m[nameof(_parameter1)] = _parameter1;
            m[nameof(_parameter2)] = _parameter2;
            m[nameof(_method)] = _method;
        }

        private static CActionWithSpecifiedParameters<T1, T2> Deserialize(RMap rm)
        {
            return new CActionWithSpecifiedParameters<T1, T2>(
                    rm.Get<Action<T1, T2>>(nameof(_method)),
                    rm.Get<T1>(nameof(_parameter1)),
                    rm.Get<T2>(nameof(_parameter2))
                ) { _serialized = true };
        }

        public static implicit operator Action(CActionWithSpecifiedParameters<T1, T2> a) => a.Action;
    }
}
