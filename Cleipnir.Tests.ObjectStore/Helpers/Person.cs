using Cleipnir.ObjectDB.Version2.Persistency;
using Cleipnir.ObjectDB.Version2.Persistency.Serializers;

namespace Cleipnir.Tests.ObjectStore.Helpers
{
    public class Person : IPersistable2
    {
        public string Name { get; set; }
        public Person Relationship { get; set; }
        
        public void Serialize(Map2 m)
        {
            m[nameof(Name)] = Name;
            m[nameof(Relationship)] = Relationship;
        }

        private static Person Deserialize(RMap rm)
        {
            var person = new Person
            {
                Name = rm.Get<string>(nameof(Name))
            };

            rm.WhenResolved<Person>(nameof(Relationship), p => person.Relationship = p);
            return person;
        }
    }
}