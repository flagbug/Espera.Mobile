using Android.Content;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;

namespace Espera.Android.Views
{
    public class RemoteArtistsFragment : ArtistsFragment<NetworkSong>
    {
        public override void OnResume()
        {
            base.OnResume();

            this.Activity.SetTitle(Resource.String.remote_artists_fragment_title);
        }

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