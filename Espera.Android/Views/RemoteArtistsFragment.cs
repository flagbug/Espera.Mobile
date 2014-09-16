using Android.Content;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;

namespace Espera.Android.Views
{
    public class RemoteArtistsFragment : ArtistsFragment<NetworkSong>
    {
        protected override ArtistsViewModel<NetworkSong> ConstructViewModel()
        {
            return new RemoteArtistsViewModel();
        }

        protected override void OpenArtist()
        {
            var intent = new Intent(this.Activity, typeof(RemoteSongsActivity));
            this.StartActivity(intent);
        }
    }
}