using System;
using Android.App;
using Android.Runtime;
using Espera.Mobile.Core.Network;
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
        }
    }
}