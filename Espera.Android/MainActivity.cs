using Android.App;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.OS;
using Android.Widget;
using ReactiveUI;
using ReactiveUI.Android;
using ReactiveUI.Mobile;
using System;
using System.Reactive.Linq;
using Xamarin.ActionbarSherlockBinding;
using Xamarin.ActionbarSherlockBinding.Views;

namespace Espera.Android
{
    [Activity(Label = "Espera", MainLauncher = true, Icon = "@drawable/icon",
        ConfigurationChanges = ConfigChanges.Orientation)]
    public class MainActivity : ReactiveActivity<MainViewModel>, ActionBarSherlock.IOnCreateOptionsMenuListener
    {
        private readonly AutoSuspendActivityHelper autoSuspendHelper;
        private readonly ActionBarSherlock sherlock;

        public MainActivity()
        {
            this.autoSuspendHelper = new AutoSuspendActivityHelper(this);
            this.sherlock = ActionBarSherlock.Wrap(this);
        }

        private Button ConnectButton
        {
            get { return this.FindViewById<Button>(Resource.Id.connectButton); }
        }

        private Button LoadArtistsButton
        {
            get { return this.FindViewById<Button>(Resource.Id.loadArtistsButton); }
        }

        private Button LoadCurrentPlaylistButton
        {
            get { return this.FindViewById<Button>(Resource.Id.loadCurrentPlaylistButton); }
        }

        public override bool OnCreateOptionsMenu(global::Android.Views.IMenu menu)
        {
            return sherlock.DispatchCreateOptionsMenu(menu);
        }

        public bool OnCreateOptionsMenu(IMenu menu)
        {
            menu.Add("Settings");

            return true;
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            this.autoSuspendHelper.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Main);

            if (this.Intent.HasExtra("connectionLost"))
            {
                Toast.MakeText(this, "Connection lost", ToastLength.Long).Show();
            }

            var wifiManager = (WifiManager)this.GetSystemService(WifiService);
            if (!wifiManager.IsWifiEnabled)
            {
                var builder = new AlertDialog.Builder(this);
                builder.SetTitle("Error");
                builder.SetMessage("You have to enable Wifi.");
                builder.SetPositiveButton("Enable", (sender, args) => wifiManager.SetWifiEnabled(true));
                builder.SetNegativeButton("Exit", (sender, args) => this.Finish());

                builder.Show();
            }

            this.ViewModel = new MainViewModel();
            this.BindCommand(this.ViewModel, x => x.ConnectCommand, x => x.ConnectButton);
            this.ViewModel.ConnectCommand.IsExecuting
                .Select(x => x ? "Connecting..." : "Connect")
                .BindTo(this.ConnectButton, x => x.Text);

            this.ViewModel.ConnectionFailed.Subscribe(x => Toast.MakeText(this, "Connection failed", ToastLength.Long).Show());

            this.OneWayBind(this.ViewModel, x => x.IsConnected, x => x.LoadArtistsButton.Enabled);
            this.LoadArtistsButton.Click += (sender, args) => this.StartActivity(typeof(ArtistsActivity));

            this.OneWayBind(this.ViewModel, x => x.IsConnected, x => x.LoadCurrentPlaylistButton.Enabled);
            this.LoadCurrentPlaylistButton.Click += (sender, args) => this.StartActivity(typeof(PlaylistActivity));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            NetworkMessenger.Instance.Dispose();
        }

        protected override void OnPause()
        {
            base.OnPause();
            this.autoSuspendHelper.OnPause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            this.autoSuspendHelper.OnResume();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            this.autoSuspendHelper.OnSaveInstanceState(outState);
        }
    }
}