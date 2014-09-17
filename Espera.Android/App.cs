using System;
using System.Net.Http;
using Android.App;
using Android.Runtime;
using Espera.Android.Analytics;
using Espera.Mobile.Core;
using Espera.Mobile.Core.Analytics;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Settings;
using Espera.Mobile.Core.SongFetchers;
using ModernHttpClient;
using ReactiveUI;
using Splat;

namespace Espera.Android
{
    [Application(Label = "Espera Remote Control", Icon = "@drawable/Icon", Theme = "@style/Theme.MahApps", Logo = "@drawable/Logo",
#if DEBUG
 Debuggable = true
#else
 Debuggable = false
#endif
)]
    public class App : Application
    {
        private AutoSuspendHelper suspendHelper;

        private App(IntPtr handle, JniHandleOwnership owner)
            : base(handle, owner)
        { }

        public override void OnCreate()
        {
            base.OnCreate();

            this.suspendHelper = new AutoSuspendHelper(this);
            //RxApp.SuspensionHost.SetupDefaultSuspendResume();
            Locator.CurrentMutable.Register(() => new AndroidWifiService(), typeof(IWifiService));
            Locator.CurrentMutable.Register(() => new AndroidSongFetcher(), typeof(ISongFetcher<LocalSong>));
            Locator.CurrentMutable.Register(() => new File(), typeof(IFile));
            Locator.CurrentMutable.Register(() => new AndroidTcpClient(), typeof(ITcpClient));
            Locator.CurrentMutable.Register(() => new AndroidUdpClient(), typeof(IUdpClient));
            Locator.CurrentMutable.RegisterConstant(new AndroidAnalytics(this), typeof(IAnalytics));
            Locator.CurrentMutable.Register(() => new AndroidDeviceIdFactory(this), typeof(IDeviceIdFactory));
            Locator.CurrentMutable.RegisterConstant(new UserSettings(), typeof(UserSettings));
            Locator.CurrentMutable.RegisterConstant(new AndroidSettings(), typeof(AndroidSettings));
            Locator.CurrentMutable.Register(() => new AndroidInstallationDateFetcher(), typeof(IInstallationDateFetcher));
            Locator.CurrentMutable.Register(() => new NativeMessageHandler(), typeof(HttpMessageHandler));

#if DEBUG
            Locator.CurrentMutable.RegisterConstant(new AndroidLogger(), typeof(ILogger));
#endif
        }
    }
}