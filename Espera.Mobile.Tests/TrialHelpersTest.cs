using System;
using Espera.Mobile.Core;
using NSubstitute;
using Xunit;

namespace Espera.Android.Tests
{
    public class TrialHelpersTest
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