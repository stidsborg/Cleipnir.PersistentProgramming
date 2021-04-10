using Cleipnir.ObjectDB.Version2.Persistency;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ObjectStore.Deserialization
{
    [TestClass]
    public class CircularPathTests
    {
        [TestMethod]
        public void DetectAndReturnCircularPath()
        {
            var path = new Path();
            var (isCircular, circularPath) = path.Push(1);
            isCircular.ShouldBe(false);
            circularPath.ShouldBeEmpty();
            
            (isCircular, circularPath) = path.Push(2);
            isCircular.ShouldBe(false);
            circularPath.ShouldBeEmpty();
            
            (isCircular, circularPath) = path.Push(3);
            isCircular.ShouldBe(false);
            circularPath.ShouldBeEmpty();
            
            (isCircular, circularPath) = path.Push(4);
            isCircular.ShouldBe(false);
            circularPath.ShouldBeEmpty();
         
            (isCircular, circularPath) = path.Push(5);
            isCircular.ShouldBe(false);
            circularPath.ShouldBeEmpty();
            
            (isCircular, circularPath) = path.Push(3);
            isCircular.ShouldBe(true);
            circularPath.ShouldBe(new long[] { 3,4,5,3 });
        }
    }
}