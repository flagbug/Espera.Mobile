using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Provider;
using Espera.Mobile.Core.Songs;
using Espera.Mobile.Core.ViewModels;
using Google.Analytics.Tracking;

namespace Espera.Android.Views
{
    [Activity(Label = "Local Artists", ConfigurationChanges = ConfigChanges.Orientation)]
    public class LocalArtistsActivity : ArtistsActivity<LocalSong>
    {
        protected override ArtistsViewModel<LocalSong> ConstructViewModel()
        {
            return new ArtistsViewModel<LocalSong>(new AndroidSongFetcher(x =>
                this.ContentResolver.Query(MediaStore.Audio.Media.ExternalContentUri, x, MediaStore.Audio.Media.InterfaceConsts.IsMusic + " != 0", null, null)));
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
            var intent = new Intent(this, typeof(LocalSongsActivity));
            intent.PutExtra("songs", this.ViewModel.SerializeSongsForSelectedArtist(artist));

            this.StartActivity(intent);
        }
    }
}