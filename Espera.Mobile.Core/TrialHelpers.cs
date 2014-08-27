using System;
using Espera.Network;
using Splat;

namespace Espera.Mobile.Core
{
    public static class TrialHelpers
    {
        public static NetworkAccessPermission GetAccessPermissionForPremiumState(NetworkAccessPermission permission, bool isPremium)
        {
            if (isPremium && permission == NetworkAccessPermission.Admin)
            {
                return NetworkAccessPermission.Admin;
            }

            return NetworkAccessPermission.Guest;
        }

        public static TimeSpan GetRemainingTrialTime(TimeSpan trialTime, IClock clock = null, IInstallationDateFetcher installationDateFetcher = null)
        {
            clock = clock ?? new Clock();
            installationDateFetcher = installationDateFetcher ?? Locator.Current.GetService<IInstallationDateFetcher>();

            return installationDateFetcher.GetInstallationDate() + trialTime - clock.Now;
        }

        public static bool IsInTrialPeriod(TimeSpan trialTime, IClock clock = null, IInstallationDateFetcher installationDateFetcher = null)
        {
            return GetRemainingTrialTime(trialTime, clock, installationDateFetcher) > TimeSpan.Zero;
        }
    }
}