using Android.App;
using Android.Content.PM;
using Android.OS;
using Espera.Mobile.Core;
using Espera.Mobile.Core.SongFetchers;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
using Google.Analytics.Tracking;

namespace Espera.Android.Views
{
    [Activity(ConfigurationChanges = ConfigChanges.Orientation)]
    public class RemoteArtistsActivity : ArtistsActivity<NetworkSong>
    {
        protected override ArtistsViewModel<NetworkSong> ConstructViewModel()
        {
            return new ArtistsViewModel<NetworkSong>(new RemoteSongFetcher(), BlobCacheKeys.SelectedRemoteSongs);
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