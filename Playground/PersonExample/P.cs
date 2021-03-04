using System;
using Cleipnir.ObjectDB;
using Cleipnir.StorageEngine.SimpleFile;

namespace Playground.PersonExample
{
    internal static class P
    {
        public static void Do()
        {
            var storageEngine = new SimpleFileStorageEngine("./persons.jsons", true);
            var os = ObjectStore.New(storageEngine);
            
            var parent = new Person()
            {
                Name = "Ole"
            };

            var child = new Person()
            {
                Name = "Peter",
                Parent = parent
            };
            
            os.Entangle(child);
            os.Persist();

            os = ObjectStore.Load(storageEngine);
            var p2 = os.Resolve<Person>();
            Console.WriteLine("Child: " + p2.Name);
            Console.WriteLine("Parent: " + p2.Parent.Name);
        }
    }
}