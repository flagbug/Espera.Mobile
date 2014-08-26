using System;
using Espera.Mobile.Core;
using NSubstitute;
using Xunit;

namespace Espera.Android.Tests
{
    public class TrialHelpersTest
    {
        public class TheGetRemainingTrialTimeMethod
        {
            [Fact]
            public void ReturnsNegativeValueForExpiredTime()
            {
                var clock = Substitute.For<IClock>();
                clock.Now.Returns(DateTime.MinValue + TimeSpan.FromDays(8));

                var installationDateFetcher = Substitute.For<IInstallationDateFetcher>();
                installationDateFetcher.GetInstallationDate().Returns(DateTime.MinValue);

                Assert.Equal(TimeSpan.FromDays(-1), TrialHelpers.GetRemainingTrialTime(TimeSpan.FromDays(7), clock, installationDateFetcher));
            }

            [Fact]
            public void SmokeTest()
            {
                var clock = Substitute.For<IClock>();
                clock.Now.Returns(DateTime.MinValue + TimeSpan.FromDays(2));

                var installationDateFetcher = Substitute.For<IInstallationDateFetcher>();
                installationDateFetcher.GetInstallationDate().Returns(DateTime.MinValue + TimeSpan.FromDays(1));

                Assert.Equal(TimeSpan.FromDays(6), TrialHelpers.GetRemainingTrialTime(TimeSpan.FromDays(7), clock, installationDateFetcher));
            }
        }

        public class TheIsInTrialPeriodMethod
        {
            [Fact]
            public void ExpirationIsOnExactSameTicks()
            {
                var clock = Substitute.For<IClock>();
                clock.Now.Returns(DateTime.MinValue + AppConstants.TrialTime);

                var installationDateFetcher = Substitute.For<IInstallationDateFetcher>();
                installationDateFetcher.GetInstallationDate().Returns(DateTime.MinValue);

                Assert.False(TrialHelpers.IsInTrialPeriod(AppConstants.TrialTime, clock, installationDateFetcher));
            }

            [Fact]
            public void OneTickBeforeExpirationIsInTrialPeriod()
            {
                var clock = Substitute.For<IClock>();
                clock.Now.Returns(DateTime.MinValue + AppConstants.TrialTime - TimeSpan.FromTicks(1));

                var installationDateFetcher = Substitute.For<IInstallationDateFetcher>();
                installationDateFetcher.GetInstallationDate().Returns(DateTime.MinValue);

                Assert.True(TrialHelpers.IsInTrialPeriod(AppConstants.TrialTime, clock, installationDateFetcher));
            }
        }
    }
}