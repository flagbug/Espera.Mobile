using Android.Content;
using Espera.Mobile.Core;
using Espera.Mobile.Core.SongFetchers;
using Espera.Mobile.Core.ViewModels;
using Splat;

namespace Espera.Android.Views
{
    public class LocalArtistsFragment : ArtistsFragment<LocalSong>
    {
        protected override ArtistsViewModel<LocalSong> ConstructViewModel()
        {
            return new ArtistsViewModel<LocalSong>(Locator.Current.GetService<ISongFetcher<LocalSong>>(), BlobCacheKeys.SelectedLocalSongs);
        }

        protected override void OpenArtist()
        {
            var intent = new Intent(this.Activity, typeof(LocalSongsActivity));
            this.StartActivity(intent);
        }
    }
}