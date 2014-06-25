using Android.App;
using Android.Content;
using Android.Content.PM;
using Espera.Mobile.Core.SongFetchers;
using Espera.Mobile.Core.Songs;
using Espera.Mobile.Core.ViewModels;
using Google.Analytics.Tracking;

namespace Espera.Android.Views
{
    [Activity(Label = "Remote Artists", ConfigurationChanges = ConfigChanges.Orientation)]
    public class RemoteArtistsActivity : ArtistsActivity<RemoteSong>
    {
        protected override ArtistsViewModel<RemoteSong> ConstructViewModel()
        {
            return new ArtistsViewModel<RemoteSong>(new RemoteSongFetcher());
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

        protected override void OpenArtist(string artist)
        {
            var intent = new Intent(this, typeof(RemoteSongsActivity));
            intent.PutExtra("songs", this.ViewModel.SerializeSongsForSelectedArtist(artist));

            this.StartActivity(intent);
        }
    }
}