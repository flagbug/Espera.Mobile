using Akavache;
using Android.App;
using Android.Net.Wifi;
using Android.OS;
using Android.Widget;
using ReactiveUI;
using ReactiveUI.Android;
using System;
using System.Reactive.Linq;

namespace Espera.Android
{
    [Activity(Label = "Espera", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : ReactiveActivity<MainViewModel>
    {
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

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Main);

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

            this.ConnectButton.Click += (sender, args) => this.ViewModel.ConnectCommand.Execute(null);
            this.ViewModel.ConnectCommand.IsExecuting
                .ObserveOn(RxApp.MainThreadScheduler) // RxUI has a bug where IsExecuting is not dispatched to the UI thread
                .Select(x => x ? "Connecting..." : "Connect")
                .BindTo(this.ConnectButton, x => x.Text);
            this.ViewModel.ConnectCommand.CanExecuteObservable.BindTo(this.ConnectButton, x => x.Enabled);

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
            BlobCache.Shutdown().Wait();
        }
    }
}