using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using ReactiveUI;
using ReactiveUI.Android;
using ReactiveUI.Mobile;
using System;
using System.Reactive.Linq;

namespace Espera.Android
{
    [Activity(Label = "Current Playlist", ConfigurationChanges = ConfigChanges.Orientation)]
    public class PlaylistActivity : ReactiveActivity<PlaylistViewModel>, IHandleDisconnect
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

        private Button PlayNextSongButton
        {
            get { return this.FindViewById<Button>(Resource.Id.nextButton); }
        }

        private Button PlayPauseButton
        {
            get { return this.FindViewById<Button>(Resource.Id.playPauseButton); }
        }

        private Button PlayPreviousSongButton
        {
            get { return this.FindViewById<Button>(Resource.Id.previousButton); }
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
            this.PlaylistListView.Events().ItemClick.Select(x => x.Position).InvokeCommand(this.ViewModel.PlayPlaylistSongCommand);

            this.BindCommand(this.ViewModel, x => x.PlayNextSongCommand, x => x.PlayNextSongButton);
            this.BindCommand(this.ViewModel, x => x.PlayPreviousSongCommand, x => x.PlayPreviousSongButton);
            this.BindCommand(this.ViewModel, x => x.PlayPauseCommand, x => x.PlayPauseButton);

            this.ViewModel.WhenAnyValue(x => x.IsPlaying).Select(x => x ? Resource.Drawable.Pause : Resource.Drawable.Play)
                .Subscribe(x => this.PlayPauseButton.SetBackgroundResource(x));

            Func<bool, int> alphaSelector = x => x ? 255 : 100;

            this.ViewModel.PlayPauseCommand.CanExecuteObservable.Select(alphaSelector)
                .Subscribe(x => this.PlayPauseButton.Background.SetAlpha(x));

            this.ViewModel.PlayPreviousSongCommand.CanExecuteObservable.Select(alphaSelector)
                .Subscribe(x => this.PlayPreviousSongButton.Background.SetAlpha(x));

            this.ViewModel.PlayNextSongCommand.CanExecuteObservable.Select(alphaSelector)
                .Subscribe(x => this.PlayNextSongButton.Background.SetAlpha(x));

            NetworkMessenger.Instance.Disconnected.FirstAsync()
                .Subscribe(x => this.HandleDisconnect());

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