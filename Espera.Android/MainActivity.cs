using Android.App;
using Android.OS;
using Android.Widget;
using ReactiveUI;
using ReactiveUI.Android;
using System.Collections;
using System.Reactive.Linq;

namespace Espera.Android
{
    [Activity(Label = "Espera", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, IViewFor<MainViewModel>
    {
        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (MainViewModel)value; }
        }

        public MainViewModel ViewModel { get; set; }

        private ListView ArtistListView
        {
            get { return this.FindViewById<ListView>(Resource.Id.artistList); }
        }

        private Button DiscoverServerButton
        {
            get { return this.FindViewById<Button>(Resource.Id.discoverServerButton); }
        }

        private TextView IpAddressTextView
        {
            get { return this.FindViewById<TextView>(Resource.Id.ipAddressTextView); }
        }

        private Button LoadArtistsButton
        {
            get { return this.FindViewById<Button>(Resource.Id.loadArtistsButton); }
        }

        protected override void OnCreate(Bundle bundle)
        {
            RxApp.Initialize();
            RxApp.MainThreadScheduler = new AndroidUIScheduler(this);

            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            this.ViewModel = new MainViewModel();

            this.DiscoverServerButton.Click += (sender, args) => this.ViewModel.DiscoverServerCommand.Execute(null);
            this.ViewModel.DiscoverServerCommand.IsExecuting
                .Select(x => x ? "Discovering..." : "Discover server")
                .BindTo(this.DiscoverServerButton, x => x.Text);
            this.ViewModel.DiscoverServerCommand.CanExecuteObservable.BindTo(this.DiscoverServerButton, x => x.Enabled);

            this.OneWayBind(this.ViewModel, x => x.IpAddress, x => x.IpAddressTextView.Text);

            this.LoadArtistsButton.Click += (sender, args) => this.ViewModel.LoadArtistsCommand.Execute(null);
            this.ViewModel.LoadArtistsCommand.IsExecuting
                .Select(x => x ? "Loading..." : "Load artists")
                .BindTo(this.LoadArtistsButton, x => x.Text);
            this.ViewModel.LoadArtistsCommand.CanExecuteObservable.BindTo(this.LoadArtistsButton, x => x.Enabled);

            this.OneWayBind(this.ViewModel, x => x.Artists, x => x.ArtistListView.Adapter,
                list => new ArrayAdapter(this, global::Android.Resource.Layout.SimpleListItem1, (IList)list));
        }
    }
}