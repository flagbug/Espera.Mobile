using Akavache;
using Android.App;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using Android.Widget;
using ReactiveUI;
using ReactiveUI.Android;
using System.Reactive.Linq;

namespace Espera.Android
{
    [Activity(Label = "Espera", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : ReactiveActivity<MainViewModel>
    {
        private ListView ArtistListView
        {
            get { return this.FindViewById<ListView>(Resource.Id.artistList); }
        }

        private Button LoadArtistsButton
        {
            get { return this.FindViewById<Button>(Resource.Id.loadArtistsButton); }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

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

            this.LoadArtistsButton.Click += (sender, args) => this.ViewModel.LoadArtistsCommand.Execute(null);
            this.ViewModel.LoadArtistsCommand.IsExecuting
                .Select(x => x ? "Loading..." : "Load artists")
                .BindTo(this.LoadArtistsButton, x => x.Text);
            this.ViewModel.LoadArtistsCommand.CanExecuteObservable.BindTo(this.LoadArtistsButton, x => x.Enabled);

            this.OneWayBind(this.ViewModel, x => x.Artists, x => x.ArtistListView.Adapter,
                list => new ArtistsAdapter(this, list));
            this.ArtistListView.ItemClick += (sender, args) =>
                this.OpenArtist((string)this.ArtistListView.GetItemAtPosition(args.Position));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            NetworkMessenger.Instance.Dispose();
            BlobCache.Shutdown().Wait();
        }

        private void OpenArtist(string selectedArtist)
        {
            this.ViewModel.SelectedArtist = selectedArtist;

            var intent = new Intent(this, typeof(SongsActivity));
            intent.PutExtra("artist", selectedArtist);

            this.StartActivity(intent);
        }
    }
}