using Cleipnir.Rx;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Cleipnir.Tests.ReactiveTests
{
    [TestClass]
    public class NextTests
    {
        [TestMethod]
        public void SubscriptionIsTornDownAfterSingleEmitAndSingleNextSubscription()
        {
            var count = 0;
            var source = new Source<int>();
            var awaitable = source.Do(i => count += i).Next();
            awaitable.Completed.ShouldBeFalse();
            source.Emit(5);
            awaitable.Completed.ShouldBeTrue();
            
            source.Emit(10);
            
            count.ShouldBe(5);
        }
    }
}