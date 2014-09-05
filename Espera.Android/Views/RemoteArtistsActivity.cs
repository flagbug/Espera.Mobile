using Android.App;
using Android.Content.PM;
using Android.OS;
using Espera.Mobile.Core;
using Espera.Mobile.Core.SongFetchers;
using Espera.Mobile.Core.Songs;
using Espera.Mobile.Core.ViewModels;
using Google.Analytics.Tracking;

namespace Espera.Android.Views
{
    [Activity(ConfigurationChanges = ConfigChanges.Orientation)]
    public class RemoteArtistsActivity : ArtistsActivity<RemoteSong>
    {
        protected override ArtistsViewModel<RemoteSong> ConstructViewModel()
        {
            return new ArtistsViewModel<RemoteSong>(new RemoteSongFetcher(), BlobCacheKeys.SelectedRemoteSongs);
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.ActionBar.SetTitle(Resource.String.remote_artists);
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

        protected override void OpenArtist()
        {
            this.StartActivity(typeof(RemoteSongsActivity));
        }
    }
}