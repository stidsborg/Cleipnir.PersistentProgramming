using System;

namespace Cleipnir.ExecutionEngine.DataStructures
{
    internal struct CAction
    {
        public CAction(Action action, bool isPersistable)
        {
            Action = action;
            IsPersistable = isPersistable;
        }

        public Action Action { get; }
        public bool IsPersistable { get; }
    }
}
