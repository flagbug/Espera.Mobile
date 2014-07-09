using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.OS;
using Espera.Android.Services;
using Espera.Mobile.Core.UI;
using Google.Analytics.Tracking;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

namespace Espera.Android.Views
{
    [Activity(Label = "Espera", MainLauncher = true, Icon = "@drawable/icon",
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation, LaunchMode = LaunchMode.SingleTop)]
    public class MainActivity : AndroidActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Forms.Init(this, bundle);

            this.SetPage(XamFormsApp.GetMainPage());

            this.StartService(new Intent(this, typeof(NetworkService)));
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            this.Intent = intent;
        }

        protected override void OnResume()
        {
            base.OnResume();
            /*
            if (this.Intent.HasExtra("connectionLost"))
            {
                Toast.MakeText(this, "Connection lost", ToastLength.Long).Show();
                this.Intent.RemoveExtra("connectionLost");
            }

            var wifiManager = (WifiManager)this.GetSystemService(WifiService);

            if (!wifiManager.IsWifiEnabled)
            {
                this.ShowWifiPrompt(wifiManager);
            }

            else
            {
                WifiInfo info = wifiManager.ConnectionInfo;

                var analytics = new AndroidAnalytics(this.ApplicationContext);
                int wifiSpeed = info.LinkSpeed;
                analytics.RecordWifiSpeed(wifiSpeed);

                this.ViewModel.LocalAddress = new IPAddress(info.IpAddress);
            }*/
        }

        protected override void OnStart()
        {
            base.OnStart();

            EasyTracker.GetInstance(this).ActivityStart(this);
        }

        protected override void OnStop()
        {
            base.OnStop();

            EasyTracker.GetInstance(this).ActivityStop(this);
        }

        private void ShowWifiPrompt(WifiManager wifiManager)
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle("Error");
            builder.SetMessage("You have to enable Wifi.");
            builder.SetPositiveButton("Enable", (sender, args) => wifiManager.SetWifiEnabled(true));
            builder.SetNegativeButton("Exit", (sender, args) => this.Finish());

            builder.Show();
        }
    }
}