using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using ReactiveUI;
using ReactiveUI.Android;
using ReactiveUI.Mobile;
using System;

namespace Espera.Android
{
    [Activity(Label = "Current Playlist", ConfigurationChanges = ConfigChanges.Orientation)]
    public class PlaylistActivity : ReactiveActivity<PlaylistViewModel>
    {
        private readonly AutoSuspendActivityHelper autoSuspendHelper;

        public PlaylistActivity()
        {
            this.autoSuspendHelper = new AutoSuspendActivityHelper(this);
        }

        private ListView PlaylistListView
        {
            get { return this.FindViewById<ListView>(Resource.Id.playList); }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            this.autoSuspendHelper.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Playlist);

            this.ViewModel = new PlaylistViewModel();
            this.ViewModel.Message.Subscribe(x => Toast.MakeText(this, x, ToastLength.Short).Show());

            this.OneWayBind(this.ViewModel, x => x.Playlist, x => x.PlaylistListView.Adapter,
                playlist => playlist == null ? null : new PlaylistAdapter(this, playlist));
            this.PlaylistListView.ItemClick += (sender, args) => this.ViewModel.PlayPlaylistSongCommand.Execute(args.Position);

            this.ViewModel.LoadPlaylistCommand.Execute(null);
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
    }
}