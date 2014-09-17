using Android.Content;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
using Google.Analytics.Tracking;

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

            EasyTracker tracker = EasyTracker.GetInstance(this.Activity);
            tracker.Set(Fields.ScreenName, this.Class.Name);
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