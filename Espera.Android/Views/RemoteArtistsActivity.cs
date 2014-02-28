using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Espera.Mobile.Core.SongFetchers;
using Espera.Mobile.Core.Songs;
using Espera.Mobile.Core.ViewModels;
using Google.Analytics.Tracking;
using ReactiveUI.Mobile;

namespace Espera.Android.Views
{
    [Activity(Label = "Artists", ConfigurationChanges = ConfigChanges.Orientation)]
    public class RemoteArtistsActivity : ArtistsActivity<RemoteSong>
    {
        private readonly AutoSuspendActivityHelper autoSuspendHelper;

        public RemoteArtistsActivity()
        {
            this.autoSuspendHelper = new AutoSuspendActivityHelper(this);
        }

        protected override ArtistsViewModel<RemoteSong> ConstructViewModel()
        {
            return new ArtistsViewModel<RemoteSong>(new RemoteSongFetcher());
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            this.autoSuspendHelper.OnCreate(bundle);
        }

        protected override void OnPause()
        {
            base.OnPause();
            this.autoSuspendHelper.OnPause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            this.autoSuspendHelper.OnResume();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            this.autoSuspendHelper.OnSaveInstanceState(outState);
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
            var intent = new Intent(this, typeof(RemoteSongsActivity));
            intent.PutExtra("songs", this.ViewModel.SerializeSongsForSelectedArtist(artist));

            this.StartActivity(intent);
        }
    }
}