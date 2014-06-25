using Android.App;
using ReactiveUI;

namespace Espera.Android
{
    public class App : Application
    {
        private AutoSuspendHelper suspendHelper;

        public override void OnCreate()
        {
            base.OnCreate();

            this.suspendHelper = new AutoSuspendHelper(this);
            RxApp.SuspensionHost.SetupDefaultSuspendResume();
        }
    }
}