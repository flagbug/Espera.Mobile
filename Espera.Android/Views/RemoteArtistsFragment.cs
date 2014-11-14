using Android.Content;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
using Xamarin;

namespace Espera.Android.Views
{
    public class RemoteArtistsFragment : ArtistsFragment<NetworkSong>
    {
        public override void OnResume()
        {
            base.OnResume();

            this.Activity.SetTitle(Resource.String.remote_artists_fragment_title);
        }

        public override void OnStart()
        {
            base.OnStart();

            Insights.Track(this.GetType().Name);
        }

        protected override ArtistsViewModel<NetworkSong> ConstructViewModel() => new RemoteArtistsViewModel();

        protected override void OpenArtist()
        {
            var intent = new Intent(this.Activity, typeof(RemoteSongsActivity));
            this.StartActivity(intent);
        }
    }
}