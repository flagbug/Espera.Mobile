using Android.App;
using Android.Runtime;
using Espera.Mobile.Core;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Settings;
using Espera.Mobile.Core.SongFetchers;
using ModernHttpClient;
using ReactiveUI;
using Splat;
using System;
using System.Net.Http;
using Xamarin;

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
            Locator.CurrentMutable.Register(() => new AndroidDeviceIdFactory(this), typeof(IDeviceIdFactory));
            Locator.CurrentMutable.RegisterConstant(new UserSettings(), typeof(UserSettings));
            Locator.CurrentMutable.RegisterConstant(new AndroidSettings(), typeof(AndroidSettings));
            Locator.CurrentMutable.Register(() => new AndroidInstallationDateFetcher(), typeof(IInstallationDateFetcher));
            Locator.CurrentMutable.RegisterLazySingleton(() => new NativeMessageHandler(), typeof(HttpMessageHandler));

#if DEBUG
            Locator.CurrentMutable.RegisterConstant(new AndroidLogger(), typeof(ILogger));
            Insights.Initialize(Insights.DebugModeKey, this.ApplicationContext);
#else
            Insights.Initialize("9251496bfa10cea251b633c46bfdbd56cf6ef82a", this.ApplicationContext);
#endif
        }
    }
}