using System;
using Espera.Mobile.Core;
using Espera.Network;
using NSubstitute;
using Xunit;

namespace Espera.Android.Tests
{
    public class TrialHelpersTest
    {
        public class TheGetAccessPermissionForPremiumStateMethod
        {
            [Fact]
            public void NotPremiumAndAdminReturnsGuest()
            {
                Assert.Equal(NetworkAccessPermission.Guest,
                    TrialHelpers.GetAccessPermissionForPremiumState(NetworkAccessPermission.Admin, false));
            }

            [Fact]
            public void NotPremiumAndGuestReturnsGuest()
            {
                Assert.Equal(NetworkAccessPermission.Guest,
                    TrialHelpers.GetAccessPermissionForPremiumState(NetworkAccessPermission.Guest, false));
            }

            [Fact]
            public void PremiumAndAdminReturnsAdmin()
            {
                Assert.Equal(NetworkAccessPermission.Admin,
                    TrialHelpers.GetAccessPermissionForPremiumState(NetworkAccessPermission.Admin, true));
            }

            [Fact]
            public void PremiumAndGuestReturnsGuest()
            {
                Assert.Equal(NetworkAccessPermission.Guest,
                    TrialHelpers.GetAccessPermissionForPremiumState(NetworkAccessPermission.Guest, true));
            }
        }

        public class TheGetRemainingTrialTimeMethod
        {
            [Fact]
            public void ReturnsNegativeValueForExpiredTime()
            {
                var clock = Substitute.For<IClock>();
                clock.Now.Returns(DateTimeOffset.MinValue + TimeSpan.FromDays(8));

                var installationDateFetcher = Substitute.For<IInstallationDateFetcher>();
                installationDateFetcher.GetInstallationDate().Returns(DateTimeOffset.MinValue);

                Assert.Equal(TimeSpan.FromDays(-1), TrialHelpers.GetRemainingTrialTime(TimeSpan.FromDays(7), clock, installationDateFetcher));
            }

            [Fact]
            public void SmokeTest()
            {
                var clock = Substitute.For<IClock>();
                clock.Now.Returns(DateTimeOffset.MinValue + TimeSpan.FromDays(2));

                var installationDateFetcher = Substitute.For<IInstallationDateFetcher>();
                installationDateFetcher.GetInstallationDate().Returns(DateTimeOffset.MinValue + TimeSpan.FromDays(1));

                Assert.Equal(TimeSpan.FromDays(6), TrialHelpers.GetRemainingTrialTime(TimeSpan.FromDays(7), clock, installationDateFetcher));
            }
        }

        public class TheIsInTrialPeriodMethod
        {
            [Fact]
            public void ExpirationIsOnExactSameTicks()
            {
                var clock = Substitute.For<IClock>();
                clock.Now.Returns(DateTimeOffset.MinValue + AppConstants.TrialTime);

                var installationDateFetcher = Substitute.For<IInstallationDateFetcher>();
                installationDateFetcher.GetInstallationDate().Returns(DateTimeOffset.MinValue);

                Assert.False(TrialHelpers.IsInTrialPeriod(AppConstants.TrialTime, clock, installationDateFetcher));
            }

            [Fact]
            public void OneTickBeforeExpirationIsInTrialPeriod()
            {
                var clock = Substitute.For<IClock>();
                clock.Now.Returns(DateTimeOffset.MinValue + AppConstants.TrialTime - TimeSpan.FromTicks(1));

                var installationDateFetcher = Substitute.For<IInstallationDateFetcher>();
                installationDateFetcher.GetInstallationDate().Returns(DateTimeOffset.MinValue);

                Assert.True(TrialHelpers.IsInTrialPeriod(AppConstants.TrialTime, clock, installationDateFetcher));
            }
        }
    }
}