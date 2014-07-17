using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Espera.Mobile.Core.UI;
using Xamarin.Forms.Platform.Android;

namespace Espera.Android.Views
{
    [Activity(Label = "Espera", MainLauncher = true, Icon = "@drawable/icon",
        ConfigurationChanges = ConfigChanges.Orientation, LaunchMode = LaunchMode.SingleTop)]
    public class MainActivity : AndroidActivity
    {
        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            return AndroidVolumeRequests.Instance.HandleKeyCode(keyCode) || base.OnKeyDown(keyCode, e);
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Xamarin.Forms.Forms.Init(this, bundle);

            SetPage(XamFormsApp.GetMainPage());
        }
    }
}