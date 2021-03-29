namespace Cleipnir.ObjectDB.Version2.Persistency
{
    public interface ISerializerFactory
    {
        ISerializer2 CreateSerializer(object instance);
        bool CanSerialize(object instance);
        ISerializer2 CreateSerializer(RMap rm, Ephemerals eps);
    }
    
    public interface ISerializer2
    {
        public object Instance { get; }
        void SerializeInto(Map2 m);
    }
}