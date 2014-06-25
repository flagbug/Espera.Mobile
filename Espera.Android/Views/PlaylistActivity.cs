using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
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

                var adapter = new ReactiveListAdapter<PlaylistEntryViewModel>(this.ViewModel.Entries, (vm, parent) => new PlaylistEntryView(this, vm, parent))
                    .DisposeWith(disposable);
                this.Playlist.Adapter = adapter;
                this.Playlist.Events().ItemClick.Select(x => x.Position)
                    .InvokeCommand(this.ViewModel.PlayPlaylistSongCommand)
                    .DisposeWith(disposable);

                this.ViewModel.CanModify.CombineLatest(this.ViewModel.WhenAnyValue(x => x.CurrentSong), this.ViewModel.WhenAnyValue(x => x.RemainingVotes), Tuple.Create)
                    .SampleAndCombineLatest(this.Playlist.Events().ItemLongClick.Select(x => x.Position), (tuple, position) =>
                        new { CanModify = tuple.Item1, CurrenSong = tuple.Item2, RemainingVotes = tuple.Item3, Position = position })
                    .Subscribe(x =>
                    {
                        bool canVote = x.CurrenSong.IsVoteAble && x.RemainingVotes.HasValue;
                        bool hasVotesLeft = x.RemainingVotes > 0;
                        string voteString = hasVotesLeft ?
                            string.Format("Vote ({0})", x.RemainingVotes) : "No votes left";

                        if (x.CanModify)
                        {
                            var builder = new AlertDialog.Builder(this);
                            var items = new List<string> { "Play", "Remove", "Move Up", "Move Down" };

                            if (canVote)
                            {
                                items.Add(voteString);
                            }

                            builder.SetItems(items.ToArray(), (o, eventArgs) =>
                            {
                                switch (eventArgs.Which)
                                {
                                    case 0:
                                        this.ViewModel.PlayPlaylistSongCommand.Execute(x.Position);
                                        break;

                                    case 1:
                                        this.ViewModel.RemoveSongCommand.Execute(x.Position);
                                        break;

                                    case 2:
                                        this.ViewModel.MoveSongUpCommand.Execute(x.Position);
                                        break;

                                    case 3:
                                        this.ViewModel.MoveSongDownCommand.Execute(x.Position);
                                        break;

                                    case 4:
                                        if (hasVotesLeft)
                                        {
                                            this.ViewModel.VoteCommand.Execute(x.Position);
                                        }
                                        break;
                                }
                            });
                            builder.Create().Show();
                        }

                        else if (canVote)
                        {
                            var builder = new AlertDialog.Builder(this);
                            builder.SetItems(new[] { voteString }, (sender, args) =>
                            {
                                if (hasVotesLeft)
                                {
                                    this.ViewModel.VoteCommand.Execute(x.Position);
                                }
                            });
                            builder.Create().Show();
                        }
                    }).DisposeWith(disposable);
                this.Playlist.EmptyView = this.FindViewById(global::Android.Resource.Id.Empty);

                this.ViewModel.CanModify.Select(x => x ? ViewStates.Visible : ViewStates.Gone)
                    .BindTo(this.PlaybackControlPanel, x => x.Visibility)
                    .DisposeWith(disposable);

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
                this.progressDialog.SetMessage("Loading playlist");
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

        public Button NextButton { get; private set; }

        public LinearLayout PlaybackControlPanel { get; private set; }

        public ListView Playlist { get; private set; }

        public Button PlayPauseButton { get; private set; }

        public Button PreviousButton { get; private set; }

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