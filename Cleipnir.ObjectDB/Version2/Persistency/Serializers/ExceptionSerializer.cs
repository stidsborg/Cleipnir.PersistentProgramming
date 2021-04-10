using System;
using Newtonsoft.Json;

namespace Cleipnir.ObjectDB.Version2.Persistency.Serializers
{
    internal class ExceptionSerializer : ISerializer2, ISerializerFactory
    {
        public object Instance => Exception;

        private Exception Exception { get; init; }

        private bool _serialized;
        private static readonly JsonSerializerSettings SerializerSettings = new() { TypeNameHandling = TypeNameHandling.All };
        
        public void SerializeInto(Map2 m)
        {
            if (_serialized) return; _serialized = true;

            m[nameof(Exception)] = JsonConvert.SerializeObject(Exception, Formatting.Indented, SerializerSettings);
        }

        public bool CanSerialize(object instance) => instance is Exception;

        public ISerializer2 CreateSerializer(object instance) 
            => new ExceptionSerializer { Exception = (Exception) instance };

        public ISerializer2 CreateSerializer(RMap rm, Ephemerals eps)
            => new ExceptionSerializer
            {
                _serialized = true,
                Exception = JsonConvert.DeserializeObject<Exception>(rm.Get<string>(nameof(Exception)), SerializerSettings)
            };
    }
}
