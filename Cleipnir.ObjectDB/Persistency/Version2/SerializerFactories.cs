using System;
using System.Collections.Generic;
using System.Linq;

namespace Cleipnir.ObjectDB.Persistency.Version2
{
    public class SerializerFactories
    {
        private readonly IReadOnlyList<ISerializerFactory> _factories;

        public SerializerFactories(IReadOnlyList<ISerializerFactory> factories)
        {
            _factories = factories;
        }

        public ISerializerFactory Find(object o) => 
            _factories.FirstOrDefault(f => f.CanSerialize(o));

        public bool IsSerializable(object o) => _factories.Any(f => f.CanSerialize(o));

        public ISerializerFactory Find(Type type)
            => _factories.FirstOrDefault(f => f.GetType() == type);
    }
}