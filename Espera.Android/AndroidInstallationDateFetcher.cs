using System;
using Android.App;
using Android.Content.PM;
using Espera.Mobile.Core;

namespace Espera.Android
{
    internal class AndroidInstallationDateFetcher : IInstallationDateFetcher
    {
        public DateTimeOffset GetInstallationDate()
        {
            long installationDateLong = Application.Context.PackageManager
                .GetPackageInfo(Application.Context.PackageName, PackageInfoFlags.MetaData).FirstInstallTime;

            return new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.FromMilliseconds(installationDateLong));
        }
    }
}