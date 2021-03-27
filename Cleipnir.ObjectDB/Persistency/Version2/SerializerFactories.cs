using System;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Persistency.Version2.Serializers.Delegate;
using Cleipnir.ObjectDB.Persistency.Version2.Serializers.Persistable;

namespace Cleipnir.ObjectDB.Persistency.Version2
{
    public class SerializerFactories
    {
        private readonly IReadOnlyList<ISerializerFactory> _factories;

        public SerializerFactories(IEnumerable<ISerializerFactory> factories)
        {
            _factories = factories.ToList();
        }

        public ISerializerFactory Find(object o) => 
            _factories.FirstOrDefault(f => f.CanSerialize(o));

        public bool IsSerializable(object o) => _factories.Any(f => f.CanSerialize(o));

        public ISerializerFactory Find(Type type)
            => _factories.First(f => f.GetType() == type);

        internal static IEnumerable<ISerializerFactory> DefaultFactories { get; } =
            new ISerializerFactory[] {new DelegateSerializerFactory(), new PersistableSerializerFactory()};
        
        internal static SerializerFactories Default { get; } = new SerializerFactories(DefaultFactories);
    }
}