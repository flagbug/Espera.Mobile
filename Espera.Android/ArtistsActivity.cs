using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using ReactiveUI;
using ReactiveUI.Android;

namespace Espera.Android
{
    [Activity(Label = "Artists")]
    public class ArtistsActivity : ReactiveActivity<ArtistsViewModel>
    {
        private ListView ArtistListView
        {
            get { return this.FindViewById<ListView>(Resource.Id.artistList); }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Artists);

            this.ViewModel = new ArtistsViewModel();

            this.OneWayBind(this.ViewModel, x => x.Artists, x => x.ArtistListView.Adapter, list => new ArtistsAdapter(this, list));
            this.ArtistListView.ItemClick += (sender, args) =>
                this.OpenArtist((string)this.ArtistListView.GetItemAtPosition(args.Position));

            this.ViewModel.Load.Execute(null);
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