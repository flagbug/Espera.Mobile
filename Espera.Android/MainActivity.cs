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
            RxApp.Initialize();

            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            this.ViewModel = new MainViewModel();

            this.LoadArtistsButton.Click += (sender, args) => this.ViewModel.LoadArtistsCommand.Execute(null);
            this.ViewModel.LoadArtistsCommand.IsExecuting
                .Select(x => x ? "Loading..." : "Load artists")
                .BindTo(this.LoadArtistsButton, x => x.Text);
            this.ViewModel.LoadArtistsCommand.CanExecuteObservable.BindTo(this.LoadArtistsButton, x => x.Enabled);

            this.OneWayBind(this.ViewModel, x => x.Artists, x => x.ArtistListView.Adapter,
                list => new ArrayAdapter(this, global::Android.Resource.Layout.SimpleListItem1, (IList)list));
            this.ArtistListView.ItemClick += (sender, args) =>
                this.ViewModel.SelectedArtist = (string)this.ArtistListView.GetItemAtPosition(args.Position);
        }
    }
}