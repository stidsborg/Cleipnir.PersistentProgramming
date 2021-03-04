using System;
using Cleipnir.ObjectDB;
using Cleipnir.StorageEngine.InMemory;

namespace Playground.SayerExample
{
    public static class P
    {
        public static void Do()
        {
            var storageEngine = new InMemoryStorageEngine();
            var os = ObjectStore.New(storageEngine);
            var sayer = new Sayer();
            sayer.Greeting = "Hello";
            Action greet = sayer.Greet;
            
            os.Entangle(sayer);
            os.Entangle(greet);
            
            os.Persist();

            os = ObjectStore.Load(storageEngine);

            greet = os.Resolve<Action>();
            greet();
            os.Resolve<Sayer>().Greeting = "G'day";
            greet();
        }
    }
}