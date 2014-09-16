using System;
using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Akavache;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using Android.Views;
using Android.Widget;
using Espera.Android.Services;
using Espera.Mobile.Core;
using Espera.Mobile.Core.Analytics;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Settings;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
using Google.Analytics.Tracking;
using Humanizer;
using ReactiveMarrow;
using ReactiveUI;
using Splat;
using IMenuItem = Android.Views.IMenuItem;

namespace Espera.Android.Views
{
    [Activity(Label = "Espera", MainLauncher = true, Icon = "@drawable/icon", LaunchMode = LaunchMode.SingleTop)]
    public class MainActivity : ReactiveActivity<MainViewModel>
    {
        private ActionBarDrawerToggle drawerToggle;

        public DrawerLayout MainDrawer { get; private set; }

        public ListView MainDrawerListView { get; private set; }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            menu.Add(Resources.GetString(Resource.String.settings))
                .SetIcon(Resource.Drawable.Settings)
                .SetShowAsAction(ShowAsAction.Always);

            return true;
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            return AndroidVolumeRequests.Instance.HandleKeyCode(keyCode) || base.OnKeyDown(keyCode, e);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (this.drawerToggle.OnOptionsItemSelected(item))
            {
                return true;
            }

            this.StartActivity(typeof(SettingsActivity));

            return true;
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.Title = String.Empty;

            this.SetContentView(Resource.Layout.Main);
            this.WireUpControls();

            this.drawerToggle = new ActionBarDrawerToggle(this, this.MainDrawer, Resource.Drawable.ic_drawer, Resource.String.disconnect, Resource.String.disconnect);
            this.MainDrawer.SetDrawerListener(this.drawerToggle);
            this.MainDrawer.SetDrawerShadow(Resource.Drawable.drawer_shadow, (int)GravityFlags.Start);

            string[] drawerItems = this.Resources.GetStringArray(Resource.Array.main_drawer_items);

            this.MainDrawerListView.Adapter = new ArrayAdapter<string>(this, global::Android.Resource.Layout.SimpleListItem1, drawerItems);

            this.ActionBar.SetDisplayHomeAsUpEnabled(true);
            this.ActionBar.SetHomeButtonEnabled(true);

            FragmentManager.BeginTransaction()
                .Replace(Resource.Id.ContentFrame, new MainFragment())
                .Commit();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            BlobCache.LocalMachine.Vacuum().Wait();
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            this.Intent = intent;
        }

        protected override void OnPostCreate(Bundle savedInstanceState)
        {
            base.OnPostCreate(savedInstanceState);

            this.drawerToggle.SyncState();
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (this.Intent.HasExtra(NetworkService.ConnectionLostString))
            {
                Toast.MakeText(this, Resource.String.connection_lost, ToastLength.Long).Show();
                this.Intent.RemoveExtra(NetworkService.ConnectionLostString);
            }

            var wifiService = Locator.Current.GetService<IWifiService>();

            if (wifiService.GetIpAddress() == null)
            {
                this.ShowWifiPrompt();
            }

            else
            {
                var analytics = Locator.Current.GetService<IAnalytics>();
                analytics.RecordWifiSpeed(wifiService.GetWifiSpeed());
            }
        }

        protected override void OnStart()
        {
            base.OnStart();

            EasyTracker.GetInstance(this).ActivityStart(this);

            this.StartService(new Intent(this, typeof(NetworkService)));
        }

        protected override void OnStop()
        {
            base.OnStop();

            EasyTracker.GetInstance(this).ActivityStop(this);

            if (!NetworkMessenger.Instance.IsConnected)
            {
                this.StopService(new Intent(this, typeof(NetworkService)));
            }
        }

        private void ShowWifiPrompt()
        {
            var wifiManager = WifiManager.FromContext(this);
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle(Resource.String.wifi_reminder_title);
            builder.SetMessage(Resource.String.wifi_reminder_message);
            builder.SetPositiveButton(Resource.String.wifi_enable_now, (sender, args) => wifiManager.SetWifiEnabled(true));
            builder.SetNegativeButton(Resource.String.wifi_enable_later, (sender, args) => { });

            builder.Show();
        }
    }
}