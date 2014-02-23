using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.OS;
using Android.Views;
using Android.Widget;
using Espera.Android.Network;
using Espera.Android.ViewModels;
using Google.Analytics.Tracking;
using ReactiveUI;
using ReactiveUI.Android;
using ReactiveUI.Mobile;
using System;
using System.Reactive.Linq;
using IMenuItem = Android.Views.IMenuItem;

namespace Espera.Android.Views
{
    [Activity(Label = "Espera", MainLauncher = true, Icon = "@drawable/icon",
        ConfigurationChanges = ConfigChanges.Orientation, LaunchMode = LaunchMode.SingleTop)]
    public class MainActivity : ReactiveActivity<MainViewModel>
    {
        private readonly AutoSuspendActivityHelper autoSuspendHelper;

        public MainActivity()
        {
            this.autoSuspendHelper = new AutoSuspendActivityHelper(this);
        }

        public Button ConnectButton { get; private set; }

        public Button LoadArtistsButton { get; private set; }

        public Button LoadCurrentPlaylistButton { get; private set; }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            menu.Add("Settings").SetIcon(Resource.Drawable.Settings)
                .SetShowAsAction(ShowAsAction.Always);

            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            this.StartActivity(typeof(SettingsActivity));

            return true;
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            this.autoSuspendHelper.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Main);
            this.WireUpControls();

            this.ViewModel = new MainViewModel();

            var connectOrDisconnectCommand = this.ViewModel.WhenAnyValue(x => x.IsConnected)
                .Select(x => x ? this.ViewModel.DisconnectCommand : this.ViewModel.ConnectCommand);
            this.ConnectButton.Events().Click.CombineLatestValue(connectOrDisconnectCommand, (args, command) => command)
                .Where(x => x.CanExecute(null))
                .Subscribe(x => x.Execute(null));
            connectOrDisconnectCommand.SelectMany(x => x.CanExecuteObservable).BindTo(this.ConnectButton, x => x.Enabled);
            this.ViewModel.ConnectCommand.IsExecuting
                .CombineLatest(this.ViewModel.WhenAnyValue(x => x.IsConnected), (connecting, connected) =>
                    connected ? "Disconnect" : connecting ? "Connecting..." : "Connect")
                .BindTo(this.ConnectButton, x => x.Text);

            this.ViewModel.ConnectionFailed.Subscribe(x => Toast.MakeText(this, x, ToastLength.Long).Show());

            this.OneWayBind(this.ViewModel, x => x.IsConnected, x => x.LoadArtistsButton.Enabled);
            this.LoadArtistsButton.Events().Click.Subscribe(x => this.StartActivity(typeof(ArtistsActivity)));

            this.OneWayBind(this.ViewModel, x => x.IsConnected, x => x.LoadCurrentPlaylistButton.Enabled);
            this.LoadCurrentPlaylistButton.Events().Click.Subscribe(x => this.StartActivity(typeof(PlaylistActivity)));

            this.StartService(new Intent(this, typeof(NetworkService)));
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            this.Intent = intent;
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

            if (this.Intent.HasExtra("connectionLost"))
            {
                Toast.MakeText(this, "Connection lost", ToastLength.Long).Show();
            }

            var wifiManager = (WifiManager)this.GetSystemService(WifiService);
            if (!wifiManager.IsWifiEnabled)
            {
                this.ShowWifiPrompt(wifiManager);
            }
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            this.autoSuspendHelper.OnSaveInstanceState(outState);
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