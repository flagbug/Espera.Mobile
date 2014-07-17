using Android.App;
using Android.Content.PM;
using Espera.Mobile.Core;
using Espera.Mobile.Core.SongFetchers;
using Espera.Mobile.Core.Songs;
using Espera.Mobile.Core.ViewModels;
using Google.Analytics.Tracking;
using Splat;

namespace Espera.Android.Views
{
    [Activity(Label = "Local Artists", ConfigurationChanges = ConfigChanges.Orientation)]
    public class LocalArtistsActivity : ArtistsActivity<LocalSong>
    {
        protected override ArtistsViewModel<LocalSong> ConstructViewModel()
        {
            return new ArtistsViewModel<LocalSong>(Locator.Current.GetService<ISongFetcher<LocalSong>>(), BlobCacheKeys.SelectedLocalSongs);
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
            this.StartActivity(typeof(LocalSongsActivity));
        }
    }
}