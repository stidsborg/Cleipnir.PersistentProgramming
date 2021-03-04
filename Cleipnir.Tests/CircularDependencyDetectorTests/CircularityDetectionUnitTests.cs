using System.Linq;
using Cleipnir.ObjectDB.Persistency;
using Cleipnir.ObjectDB.Persistency.Serialization;
using Cleipnir.ObjectDB.Persistency.Serialization.Serializers;
using Cleipnir.StorageEngine.InMemory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.CircularDependencyDetectorTests
{
    [TestClass]
    public class CircularityDetectionUnitTests
    {
        [TestMethod]
        public void DetectMinimumCircularDependencyBetweenTwoNodes()
        {
            var a = new Node { Name = "A" };
            var b = new Node { Name = "B" };

            a.Edges.Add(b);
            b.Edges.Add(a);

            var roots = new RootsInstance();
            roots.Entangle(a);

            var serializers = new Serializers(new SerializerFactory());
            var stateMaps = new StateMaps(serializers);
            var persister = new Persister(new InMemoryStorageEngine(), roots, serializers, stateMaps);
            persister.DetectSerializeAndPersistChanges();

            var circularChain = CircularDependencyDetector.Check(serializers[0], stateMaps, serializers);
            
            circularChain.IsCircular.ShouldBeTrue();
            circularChain.Path.Count().ShouldBe(3);
            circularChain.ToString().ShouldBe("A->B->A");
        }
        
        [TestMethod]
        public void DetectSelfCircularDependency()
        {
            var a = new Node { Name = "A" };

            a.Edges.Add(a);

            var roots = new RootsInstance();
            roots.Entangle(a);

            var serializers = new Serializers(new SerializerFactory());
            var stateMaps = new StateMaps(serializers);
            var persister = new Persister(new InMemoryStorageEngine(), roots, serializers, stateMaps);
            persister.DetectSerializeAndPersistChanges();

            var circularChain = CircularDependencyDetector.Check(serializers[0], stateMaps, serializers);
            
            circularChain.IsCircular.ShouldBeTrue();
            circularChain.Path.Count().ShouldBe(2);
            circularChain.ToString().ShouldBe("A->A");
        }
        
        [TestMethod]
        public void DetectCircularDependencyOneNodeHavingMultipleOutgoingEdges()
        {
            var a = new Node { Name = "A" };
            var b = new Node { Name = "B" };
            var c = new Node { Name = "C" };
            var d = new Node { Name = "D" };

            a.Edges.Add(b);
            a.Edges.Add(d);
            b.Edges.Add(c);
            c.Edges.Add(b);
            
            var roots = new RootsInstance();
            roots.Entangle(a);

            var serializers = new Serializers(new SerializerFactory());
            var stateMaps = new StateMaps(serializers);
            var persister = new Persister(new InMemoryStorageEngine(), roots, serializers, stateMaps);
            persister.DetectSerializeAndPersistChanges();
            
            var circularChain = CircularDependencyDetector.Check(serializers[0], stateMaps, serializers);
            circularChain.IsCircular.ShouldBeTrue();
            circularChain.Path.Count().ShouldBe(3);
            circularChain.ToString().ShouldBe("B->C->B");
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
            
            var roots = new RootsInstance();
            roots.Entangle(a);

            var serializers = new Serializers(new SerializerFactory());
            var stateMaps = new StateMaps(serializers);
            var persister = new Persister(new InMemoryStorageEngine(), roots, serializers, stateMaps);
            persister.DetectSerializeAndPersistChanges();
            
            var circularChain = CircularDependencyDetector.Check(serializers[0], stateMaps, serializers);
            circularChain.IsCircular.ShouldBe(true);
            circularChain.Path.Count().ShouldBe(4);
            circularChain.ToString().ShouldBe("F->E->D->F");
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
            
            var roots = new RootsInstance();
            roots.Entangle(a);

            var serializers = new Serializers(new SerializerFactory());
            var stateMaps = new StateMaps(serializers);
            var persister = new Persister(new InMemoryStorageEngine(), roots, serializers, stateMaps);
            persister.DetectSerializeAndPersistChanges();
            
            var circularChain = CircularDependencyDetector.Check(serializers[0], stateMaps, serializers);
            circularChain.IsCircular.ShouldBe(false);
        }
    }
}