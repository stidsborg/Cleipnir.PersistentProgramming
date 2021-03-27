using Cleipnir.ObjectDB.Persistency.Version2;
using Cleipnir.ObjectDB.Persistency.Version2.Serializers;
using Cleipnir.ObjectDB.Persistency.Version2.Serializers.Persistable;

namespace Cleipnir.Tests.ObjectStore
{
    public class Person : IPersistable2
    {
        public string Name { get; set; }
        public Person Other { get; set; }
        
        public void Serialize(Map2 m)
        {
            m[nameof(Name)] = Name;
            m[nameof(Other)] = Other;
        }

        private static Person Deserialize(RMap rm)
        {
            var person = new Person
            {
                Name = rm.Get<string>(nameof(Name))
            };

            rm.WhenResolved<Person>(nameof(Other), p => person.Other = p);
            return person;
        }
    }
}