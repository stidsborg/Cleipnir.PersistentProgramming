using System;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.Rx
{
    public interface IPersistableOperator<TIn, TOut> : IPersistable
    {
        void Operator(TIn next, Action<TOut> notify);
    }
}