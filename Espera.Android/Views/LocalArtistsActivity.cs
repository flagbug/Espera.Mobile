using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using Espera.Mobile.Core.Songs;
using Espera.Mobile.Core.ViewModels;
using Google.Analytics.Tracking;
using ReactiveUI.Mobile;
using System;

namespace Espera.Android.Views
{
    [Activity(Label = "Artists", ConfigurationChanges = ConfigChanges.Orientation)]
    public class LocalArtistsActivity : ArtistsActivity<LocalSong>
    {
        private readonly AutoSuspendActivityHelper autoSuspendHelper;

        public LocalArtistsActivity()
        {
            this.autoSuspendHelper = new AutoSuspendActivityHelper(this);
        }

        protected override ArtistsViewModel<LocalSong> ConstructViewModel()
        {
            return new ArtistsViewModel<LocalSong>(new AndroidSongFetcher(x =>
                this.ManagedQuery(MediaStore.Audio.Media.ExternalContentUri, x, MediaStore.Audio.Media.InterfaceConsts.IsMusic + " != 0", null, null)));
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
            throw new NotImplementedException();
        }
    }
}