using System;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ExecutionEngine.DataStructures;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Helpers;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.ExecutionEngine
{
    internal class ReadyToSchedules : IPersistable
    {
        private readonly CArray<CAction> _actions = new CArray<CAction>();

        public ReadyToSchedules() { }

        private ReadyToSchedules(CAction[] actions) => _actions = new CArray<CAction> {actions};

        public bool Empty() => _actions.Count == 0;

        public void MoveAllTo(CArray<CAction> destination) => _actions.MoveTo(destination);

        public void MoveAllFrom(CArray<CAction> source) => source.MoveTo(_actions);

        public void Enqueue(Action action, bool persistent) 
            => _actions.Add(new CAction(action, persistent));

        public void Serialize(StateMap stateToSerialize, SerializationHelper helper) 
            => _actions
                .Where(a => a.IsPersistable)
                .Select(a => a.Action)
                .SerializeInto(stateToSerialize); //todo improve performance of this

        public static ReadyToSchedules Deserialize(IReadOnlyDictionary<string, object> sm)
            => new ReadyToSchedules(sm.DeserializeIntoList<Action>()
                .Select(a => new CAction(a, true))
                .ToArray()
            );
    }
}
