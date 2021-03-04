using System.Collections.Generic;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Deserialization;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Helpers;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;

namespace Cleipnir.Tests.CircularDependencyDetectorTests
{
    internal class Node : IPersistable
    {
        public string Name { get; init; }
        public List<Node> Edges { get; private init; } = new List<Node>();

        public void Add(Node node) => Edges.Add(node);
        
        public void Serialize(StateMap sd, SerializationHelper helper)
        {
            sd[nameof(Name)] = Name;
            Edges.SerializeInto(sd);
        }

        private static Node Deserialize(IReadOnlyDictionary<string, object> sd)
            => new Node()
            {
                Name = sd.Get<string>(nameof(Name)),
                Edges = sd.DeserializeIntoList<Node>()
            };

        public override string ToString() => Name;
    }}