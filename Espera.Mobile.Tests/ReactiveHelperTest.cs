using System;
using System.Reactive;
using System.Reactive.Subjects;
using Espera.Mobile.Core;
using Microsoft.Reactive.Testing;
using ReactiveUI;
using ReactiveUI.Testing;
using Xunit;

namespace Espera.Android.Tests
{
    public class ReactiveHelperTest
    {
        public class TheThrottleWhenIncomingMethod
        {
            [Fact]
            public void SmokeTest()
            {
                var scheduler = new TestScheduler();
                var throttleDuration = TimeSpan.FromSeconds(5);

                var source = new Subject<int>();
                var throttler = new Subject<Unit>();

                var output = source.ThrottleWhenIncoming(throttler, throttleDuration, scheduler).CreateCollection();

                source.OnNext(1);
                source.OnNext(2);

                scheduler.AdvanceByMs(1000);

                Assert.Equal(1, output[0]);
                Assert.Equal(2, output[1]);

                throttler.OnNext(Unit.Default);

                source.OnNext(3);

                scheduler.AdvanceByMs(1000);

                Assert.Equal(2, output.Count);

                scheduler.AdvanceByMs(5000);

                Assert.Equal(2, output.Count);

                source.OnNext(4);

                scheduler.AdvanceByMs(1000);

                Assert.Equal(4, output[2]);
            }
        }
    }
}