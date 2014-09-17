using System;
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
using Espera.Mobile.Core.Analytics;
using Espera.Mobile.Core.Network;
using Google.Analytics.Tracking;
using ReactiveMarrow;
using ReactiveUI;
using Splat;
using Fragment = Android.App.Fragment;
using IMenuItem = Android.Views.IMenuItem;

namespace Espera.Android.Views
{
    [Activity(Label = "Espera", MainLauncher = true, Icon = "@drawable/icon", LaunchMode = LaunchMode.SingleTop)]
    public class MainActivity : ReactiveActivity
    {
        private IDisposable activationDisposable;
        private DeactivatableListAdapter<string> drawerAdapter;
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
            this.RequestWindowFeature(WindowFeatures.IndeterminateProgress);

            base.OnCreate(bundle);

            this.Title = String.Empty;

            this.SetContentView(Resource.Layout.Main);
            this.WireUpControls();

            this.drawerToggle = new ActionBarDrawerToggle(this, this.MainDrawer, Resource.Drawable.ic_drawer, Resource.String.disconnect, Resource.String.disconnect);
            this.MainDrawer.SetDrawerListener(this.drawerToggle);
            this.MainDrawer.SetDrawerShadow(Resource.Drawable.drawer_shadow, (int)GravityFlags.Start);

            string[] drawerItems = this.Resources.GetStringArray(Resource.Array.main_drawer_items);

            this.drawerAdapter = new DeactivatableListAdapter<string>(this, global::Android.Resource.Layout.SimpleListItem1, drawerItems);
            this.MainDrawerListView.Adapter = this.drawerAdapter;

            this.MainDrawerListView.Events().ItemClick
                .Subscribe(x => this.HandleNavigation(x.Position));

            this.ActionBar.SetDisplayHomeAsUpEnabled(true);
            this.ActionBar.SetHomeButtonEnabled(true);

            // Only set the connection fragment if we're freshly starting this activity and not on
            // an orientation change
            if (bundle == null)
            {
                FragmentManager.BeginTransaction()
                    .Replace(Resource.Id.ContentFrame, new MainFragment())
                    .Commit();
            }
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

            var disposable = new CompositeDisposable();

            NetworkMessenger.Instance.WhenAnyValue(x => x.IsConnected)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    // Update the enabled state of the network specific buttons
                    for (var i = 1; i < this.drawerAdapter.Count; i++)
                    {
                        this.drawerAdapter.SetIsEnabled(i, x);
                    }
                }).DisposeWith(disposable);

            this.activationDisposable = disposable;

            this.StartService(new Intent(this, typeof(NetworkService)));
        }

        protected override void OnStop()
        {
            base.OnStop();

            if (this.activationDisposable != null)
            {
                this.activationDisposable.Dispose();
            }

            if (!NetworkMessenger.Instance.IsConnected)
            {
                this.StopService(new Intent(this, typeof(NetworkService)));
            }
        }

        private void HandleNavigation(int position)
        {
            Fragment fragment = null;

            switch (position)
            {
                case 0:
                    fragment = new MainFragment();
                    break;

                case 1:
                    fragment = new PlaylistFragment();
                    break;

                case 2:
                    fragment = new RemoteArtistsFragment();
                    break;

                case 3:
                    fragment = new LocalArtistsFragment();
                    break;

                case 4:
                    fragment = new SoundCloudFragment();
                    break;

                case 5:
                    fragment = new YoutubeFragment();
                    break;
            }

            FragmentManager.BeginTransaction()
                .Replace(Resource.Id.ContentFrame, fragment)
                .Commit();

            this.MainDrawer.CloseDrawer(this.MainDrawerListView);
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