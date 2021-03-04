using Cleipnir.ObjectDB;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.CircularDependencyDetectorTests
{
    [TestClass]
    public class CircularityDetectionIntegrationTests
    {
        [TestMethod]
        public void DetectSelfCircularDependency()
        {
            var a = new Node { Name = "A" };
            a.Edges.Add(a);
            
            var os = ObjectStore.New(new InMemoryStorageEngine());
            os.Entangle(a);
            
            Should.Throw<CircularDependencyException>(() => 
                os.Persist(true)
            );
        }
        
        [TestMethod]
        public void DoNotDetectCircularDependencyOnNonCircularGraph()
        {
            var a = new Node { Name = "A" };
            var b = new Node { Name = "B" };
            a.Edges.Add(b);
            
            var os = ObjectStore.New(new InMemoryStorageEngine());
            os.Entangle(a);
            
            os.Persist(true);
        }

        [TestMethod]
        public void DetectCircularDependencyInComplexGraph()
        {
            var a = new Node { Name = "A" };
            var b = new Node { Name = "B" };
            var c = new Node { Name = "C" };
            var d = new Node { Name = "D" };
            var e = new Node { Name = "E" };
            var f = new Node { Name = "F" };
            var g = new Node { Name = "G" };
            var h = new Node { Name = "H" };
            
            a.Add(b);
            a.Add(c);
            
            c.Add(g);
            c.Add(f);
            
            g.Add(b);
            
            b.Add(d);
            
            d.Add(h);
            
            d.Add(f);
            f.Add(e);
            e.Add(d);
            
            var os = ObjectStore.New(new InMemoryStorageEngine());
            os.Entangle(a);
            
            Should.Throw<CircularDependencyException>(() => 
                os.Persist(true)
            );
        }
        
        [TestMethod]
        public void DetectNonCircularityInComplexGraph()
        {
            var a = new Node { Name = "A" };
            var b = new Node { Name = "B" };
            var c = new Node { Name = "C" };
            var d = new Node { Name = "D" };
            var e = new Node { Name = "E" };
            var f = new Node { Name = "F" };
            var g = new Node { Name = "G" };
            var h = new Node { Name = "H" };
            
            a.Add(b);
            a.Add(c);
            
            c.Add(g);
            c.Add(f);
            
            g.Add(b);
            
            b.Add(d);
            
            d.Add(h);
            
            f.Add(e);
            e.Add(d);
            
            var os = ObjectStore.New(new InMemoryStorageEngine());
            os.Entangle(a);
            
            os.Persist(true);
        }
    }
}