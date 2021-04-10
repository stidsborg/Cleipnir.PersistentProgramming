using System;
using System.Collections.Generic;
using System.Linq;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers;

namespace Cleipnir.ObjectDB.Version2.Persistency
{
    public class SerializerFactories
    {
        private readonly IReadOnlyList<ISerializerFactory> _factories;

        public SerializerFactories(IEnumerable<ISerializerFactory> factories) => _factories = factories.ToList();

        public bool IsSerializable(object o) => _factories.Any(f => f.CanSerialize(o));

        public ISerializerFactory FindFactory(object instance)
        {
            var serializerFactory = _factories.FirstOrDefault(f => f.CanSerialize(instance));

            if (serializerFactory == null)
                throw new ArgumentException($"Serialization of {instance.GetType()} not supported. Did you forget to add custom serializer?");
            
            return serializerFactory;
        }
            
        public ISerializerFactory CreateSerializerFactory(Type serializerFactoryType)
        {
            var factory = _factories.FirstOrDefault(f => f.GetType() == serializerFactoryType);

            if (factory == null)
                throw new ArgumentException($"SerializerFactory has not been registered for type: {serializerFactoryType.Name}");

            return factory;
        }
        
        internal static IEnumerable<ISerializerFactory> DefaultFactories { get; } =
            new ISerializerFactory[]
            {
                new PersistableSerializer2(),
                new ExceptionSerializer(),
                new DelegateSerializer2(),
                new DisplayClassSerializer(),
                new ListSerializerFactory(),
            };
        
        internal static SerializerFactories Default { get; } = new SerializerFactories(DefaultFactories);
    }

    public record SerializerAndFactory(ISerializer2 Serializer, ISerializerFactory Factory);
}