using System;
using Android.App;
using Android.Provider;
using Android.Runtime;
using Espera.Android.Analytics;
using Espera.Mobile.Core;
using Espera.Mobile.Core.Analytics;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.SongFetchers;
using Espera.Mobile.Core.Songs;
using ReactiveUI;
using Splat;

namespace Espera.Android
{
    [Application(Label = "Espera Remote Control", Icon = "@drawable/Icon",
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
            Locator.CurrentMutable.Register(() =>
                new AndroidSongFetcher(x =>
                    this.ContentResolver.Query(MediaStore.Audio.Media.ExternalContentUri, x,
                        MediaStore.Audio.Media.InterfaceConsts.IsMusic + " != 0", null, null)), typeof(ISongFetcher<LocalSong>));
            Locator.CurrentMutable.Register(() => new File(), typeof(IFile));
            Locator.CurrentMutable.Register(() => new AndroidTcpClient(), typeof(ITcpClient));
            Locator.CurrentMutable.Register(() => new AndroidUdpClient(), typeof(IUdpClient));
            Locator.CurrentMutable.RegisterConstant(new AndroidAnalytics(this), typeof(IAnalytics));
        }
    }
}