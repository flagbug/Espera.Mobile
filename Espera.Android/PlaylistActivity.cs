using Android.App;
using Android.OS;

namespace Espera.Android
{
    [Activity(Label = "Current Playlist")]
    public class PlaylistActivity : Activity
    {
        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Playlist playlist = await NetworkMessenger.Instance.GetCurrentPlaylist();
        }
    }
}