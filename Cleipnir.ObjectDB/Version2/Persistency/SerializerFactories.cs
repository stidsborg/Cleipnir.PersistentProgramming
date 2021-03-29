using System;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers.Delegate;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers.DisplayClass;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers.Persistable;

namespace Cleipnir.ObjectDB.Version2.Persistency
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
            new ISerializerFactory[]
            {
                new DelegateSerializerFactory(), new PersistableSerializerFactory(), new DisplayClassSerializerFactory()
            };
        
        internal static SerializerFactories Default { get; } = new SerializerFactories(DefaultFactories);
    }
}