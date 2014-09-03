using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core;
using Espera.Mobile.Core.ViewModels;
using Google.Analytics.Tracking;
using ReactiveMarrow;
using ReactiveUI;

namespace Espera.Android.Views
{
    [Activity(Label = "Current Playlist", ConfigurationChanges = ConfigChanges.Orientation)]
    public class PlaylistActivity : ReactiveActivity<PlaylistViewModel>
    {
        private ProgressDialog progressDialog;

        public PlaylistActivity()
        {
            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                this.ViewModel.Message.Subscribe(x => Toast.MakeText(this, x, ToastLength.Short).Show())
                    .DisposeWith(disposable);

                var adapter = new ReactiveListAdapter<PlaylistEntryViewModel>(this.ViewModel.Entries, (vm, parent) => new PlaylistEntryView(this, vm, parent));
                this.Playlist.Adapter = adapter;
                this.Playlist.EmptyView = this.FindViewById(global::Android.Resource.Id.Empty);
                this.Playlist.Events().ItemClick.Select(x => x.Position)
                    .Subscribe(x =>
                    {
                        this.ViewModel.SelectedEntry = this.ViewModel.Entries[x];

                        bool hasVotesLeft = this.ViewModel.RemainingVotes > 0;
                        string voteString = hasVotesLeft ?
                            string.Format("Vote ({0} {1} left)", this.ViewModel.RemainingVotes, this.ViewModel.RemainingVotes == 1 ? "vote" : "votes") :
                            Resources.GetString(Resource.String.no_votes_left);

                        if (this.ViewModel.CanModify)
                        {
                            var builder = new AlertDialog.Builder(this);
                            builder.SetTitle(Resource.String.administrator_functions);

                            var items = new List<string>
                            {
                                Resources.GetString(Resource.String.play),
                                Resources.GetString(Resource.String.remove),
                                Resources.GetString(Resource.String.move_up),
                                Resources.GetString(Resource.String.move_down),
                            };

                            if (this.ViewModel.CanVoteOnSelectedEntry)
                            {
                                items.Add(voteString);
                            }

                            builder.SetItems(items.ToArray(), async (o, eventArgs) =>
                            {
                                switch (eventArgs.Which)
                                {
                                    case 0:
                                        await this.ViewModel.PlayPlaylistSongCommand.ExecuteAsync();
                                        break;

                                    case 1:
                                        await this.ViewModel.RemoveSongCommand.ExecuteAsync();
                                        break;

                                    case 2:
                                        await this.ViewModel.MoveSongUpCommand.ExecuteAsync();
                                        break;

                                    case 3:
                                        await this.ViewModel.MoveSongDownCommand.ExecuteAsync();
                                        break;

                                    case 4:
                                        if (hasVotesLeft)
                                        {
                                            await this.ViewModel.VoteCommand.ExecuteAsync();
                                        }
                                        break;
                                }
                            });
                            builder.Create().Show();
                        }

                        else if (this.ViewModel.CanVoteOnSelectedEntry)
                        {
                            var builder = new AlertDialog.Builder(this);
                            builder.SetTitle(Resource.String.guest_functions);

                            builder.SetItems(new[] { voteString }, async (sender, args) =>
                            {
                                if (hasVotesLeft)
                                {
                                    await this.ViewModel.VoteCommand.ExecuteAsync();
                                }
                            });
                            builder.Create().Show();
                        }
                    }).DisposeWith(disposable);

                bool skipScrollEvents = false; // We use this flag to determine whether the scroll event was user or code induced
                this.WhenAnyValue(x => x.ViewModel.CurrentSong)
                    .ThrottleWhenIncoming(this.Playlist.Events().ScrollStateChanged.Where(_ => !skipScrollEvents)
                        .Where(x => x.ScrollState == ScrollState.Idle), TimeSpan.FromSeconds(10), RxApp.TaskpoolScheduler)
                    .Select(entry => this.ViewModel.Entries.TakeWhile(x => x != entry).Count())
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .SelectMany(async targetIndex =>
                    {
                        var scrollFinishedAwaiter = this.Playlist.Events().ScrollStateChanged.FirstAsync(x => x.ScrollState == ScrollState.Idle).ToTask();

                        skipScrollEvents = true;
                        this.Playlist.SmoothScrollToPosition(targetIndex);

                        await scrollFinishedAwaiter;

                        skipScrollEvents = false;

                        return Unit.Default;
                    }).Subscribe();

                this.ViewModel.WhenAnyValue(x => x.CanModify).Select(x => x ? ViewStates.Visible : ViewStates.Gone)
                    .BindTo(this.PlaybackControlPanel, x => x.Visibility)
                    .DisposeWith(disposable);

                this.ViewModel.WhenAnyValue(x => x.TotalTime.TotalSeconds).Select(x => (int)x)
                    .BindTo(this.DurationSeekBar, x => x.Max);
                this.OneWayBind(this.ViewModel, x => x.CurrentTimeSeconds, x => x.DurationSeekBar.Progress);
                this.DurationSeekBar.Events().ProgressChanged.Where(x => x.FromUser)
                    .Subscribe(x => this.ViewModel.CurrentTimeSeconds = x.Progress);

                this.ViewModel.WhenAnyValue(x => x.CurrentTimeSeconds, x => TimeSpan.FromSeconds(x).FormatAdaptive())
                    .BindTo(this.CurrentTimeTextView, x => x.Text);
                this.ViewModel.WhenAnyValue(x => x.TotalTime, x => x.FormatAdaptive())
                    .BindTo(this.TotalTimeTextView, x => x.Text);

                this.BindCommand(this.ViewModel, x => x.PlayNextSongCommand, x => x.NextButton)
                    .DisposeWith(disposable);
                this.BindCommand(this.ViewModel, x => x.PlayPreviousSongCommand, x => x.PreviousButton)
                    .DisposeWith(disposable);
                this.BindCommand(this.ViewModel, x => x.PlayPauseCommand, x => x.PlayPauseButton)
                    .DisposeWith(disposable);

                this.ViewModel.WhenAnyValue(x => x.IsPlaying).Select(x => x ? Resource.Drawable.Pause : Resource.Drawable.Play)
                    .Subscribe(x => this.PlayPauseButton.SetBackgroundResource(x))
                    .DisposeWith(disposable);

                Func<bool, int> alphaSelector = x => x ? 255 : 100;

                this.ViewModel.PlayPauseCommand.CanExecuteObservable.Select(alphaSelector)
                    .Subscribe(x => this.PlayPauseButton.Background.SetAlpha(x))
                    .DisposeWith(disposable);

                this.ViewModel.PlayPreviousSongCommand.CanExecuteObservable.Select(alphaSelector)
                    .Subscribe(x => this.PreviousButton.Background.SetAlpha(x))
                    .DisposeWith(disposable);

                this.ViewModel.PlayNextSongCommand.CanExecuteObservable.Select(alphaSelector)
                    .Subscribe(x => this.NextButton.Background.SetAlpha(x))
                    .DisposeWith(disposable);

                this.progressDialog = new ProgressDialog(this);
                this.progressDialog.SetMessage(Resources.GetString(Resource.String.loading_playlist));
                this.progressDialog.Indeterminate = true;
                this.progressDialog.SetCancelable(false);

                this.ViewModel.LoadPlaylistCommand.IsExecuting
                    .Skip(1)
                    .Subscribe(x =>
                    {
                        if (x)
                        {
                            this.progressDialog.Show();
                        }

                        else if (this.progressDialog.IsShowing)
                        {
                            this.progressDialog.Dismiss();
                        }
                    }).DisposeWith(disposable);

                this.ViewModel.LoadPlaylistCommand.Execute(null);

                return disposable;
            });
        }

        public TextView CurrentTimeTextView { get; private set; }

        public SeekBar DurationSeekBar { get; private set; }

        public Button NextButton { get; private set; }

        public LinearLayout PlaybackControlPanel { get; private set; }

        public ListView Playlist { get; private set; }

        public Button PlayPauseButton { get; private set; }

        public Button PreviousButton { get; private set; }

        public TextView TotalTimeTextView { get; private set; }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            return AndroidVolumeRequests.Instance.HandleKeyCode(keyCode) || base.OnKeyDown(keyCode, e);
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Playlist);
            this.WireUpControls();

            this.ViewModel = new PlaylistViewModel();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (this.progressDialog != null && this.progressDialog.IsShowing)
            {
                this.progressDialog.Dismiss();
            }
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
    }
}