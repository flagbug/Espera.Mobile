using System;
using System.Collections.Generic;
using System.Linq;
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
using Espera.Mobile.Core.ViewModels;
using ReactiveMarrow;
using ReactiveUI;
using Splat;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Fragment = Android.App.Fragment;
using IMenuItem = Android.Views.IMenuItem;
using Uri = Android.Net.Uri;

namespace Espera.Android.Views
{
    [Activity(Label = "Espera", MainLauncher = true, Icon = "@drawable/icon", LaunchMode = LaunchMode.SingleTop)]
    public class MainActivity : AndroidActivity
    {
        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            return AndroidVolumeRequests.Instance.HandleKeyCode(keyCode) || base.OnKeyDown(keyCode, e);
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            Forms.Init(this, bundle);

            var bootstrapper = RxApp.SuspensionHost.GetAppState<AppBootstrapper>();
            this.SetPage(bootstrapper.CreateMainPage());
        }

        /*
        private IDisposable activationDisposable;
        private MainDrawerAdapter drawerAdapter;
        private ActionBarDrawerToggle drawerToggle;

        public DrawerLayout MainDrawer { get; private set; }

        public ListView MainDrawerListView { get; private set; }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (this.drawerToggle.OnOptionsItemSelected(item))
            {
                return true;
            }

            return false;
        }

        public void OpenFeedback()
        {
            var emailIntent = new Intent(Intent.ActionSendto);
            emailIntent.SetData(Uri.Parse("mailto:daume.dennis@gmail.com"));
            emailIntent.PutExtra(Intent.ExtraSubject, "Espera Feedback");

            this.StartActivity(emailIntent);
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

            this.drawerAdapter = new MainDrawerAdapter(this, CreateMainDrawerItems());
            this.MainDrawerListView.Adapter = this.drawerAdapter;

            this.MainDrawerListView.Events().ItemClick
                .Subscribe(x => this.HandleNavigation(x.Position));

            this.ActionBar.SetDisplayHomeAsUpEnabled(true);
            this.ActionBar.SetHomeButtonEnabled(true);

            // Only set the connection fragment if we're freshly starting this activity and not on
            // an orientation change
            if (bundle == null)
            {
                this.FragmentManager.BeginTransaction()
                    .Replace(Resource.Id.ContentFrame, new ConnectionFragment())
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
                    foreach (var item in this.drawerAdapter.Where(y => y.ItemType == NavigationItemType.Primary).Skip(1))
                    {
                        item.IsEnabled = x;
                    }

                    this.drawerAdapter.NotifyDataSetChanged();
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

        private IEnumerable<NavigationItemViewModel> CreateMainDrawerItems()
        {
            return new[]
            {
                NavigationItemViewModel.CreatePrimary(this.GetString(Resource.String.main_drawer_connection), () => this.ReplaceContentFrame(new ConnectionFragment())),
                NavigationItemViewModel.CreatePrimary(this.GetString(Resource.String.main_drawer_playlist), () => this.ReplaceContentFrame(new PlaylistFragment())),
                NavigationItemViewModel.CreatePrimary(this.GetString(Resource.String.main_drawer_remote_songs), () => this.ReplaceContentFrame(new RemoteArtistsFragment())),
                NavigationItemViewModel.CreatePrimary(this.GetString(Resource.String.main_drawer_local_songs), () => this.ReplaceContentFrame(new LocalArtistsFragment())),
                NavigationItemViewModel.CreatePrimary(this.GetString(Resource.String.main_drawer_soundcloud), () => this.ReplaceContentFrame(new SoundCloudFragment())),
                NavigationItemViewModel.CreatePrimary(this.GetString(Resource.String.main_drawer_youtube), () => this.ReplaceContentFrame(new YoutubeFragment())),
                NavigationItemViewModel.CreateDivider(),
                NavigationItemViewModel.CreateSecondary(this.GetString(Resource.String.settings), Resource.Drawable.Settings, this.OpenSetting),
                NavigationItemViewModel.CreateSecondary(this.GetString(Resource.String.main_drawer_feedback), Resource.Drawable.Feedback, this.OpenFeedback)
            };
        }

        private void HandleNavigation(int position)
        {
            NavigationItemViewModel item = this.drawerAdapter[position];

            item.SelectionAction();

            this.MainDrawer.CloseDrawer(this.MainDrawerListView);
        }

        private void OpenSetting()
        {
            var settingsIntent = new Intent(this, typeof(SettingsActivity));
            this.StartActivity(settingsIntent);
        }

        private void ReplaceContentFrame(Fragment fragment)
        {
            this.FragmentManager.BeginTransaction()
                .Replace(Resource.Id.ContentFrame, fragment)
                .Commit();
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
        }*/
    }
}