using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
using Humanizer;
using ReactiveMarrow;
using ReactiveUI;

namespace Espera.Android.Views
{
    public class PlaylistFragment : ReactiveFragment<PlaylistViewModel>
    {
        private readonly Subject<IMenu> menu;

        public PlaylistFragment()
        {
            this.menu = new Subject<IMenu>();

            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                Observable.Merge(this.ViewModel.PlayPlaylistSongCommand.Select(_ => Resource.String.playing_song),
                    this.ViewModel.PlayPlaylistSongCommand.ThrownExceptions.Select(_ => Resource.String.playback_failed),
                    this.ViewModel.LoadPlaylistCommand.ThrownExceptions.Select(_ => Resource.String.loading_playlist_failed),
                    this.ViewModel.VoteCommand.ThrownExceptions.Select(_ => Resource.String.vote_failed))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(x => Toast.MakeText(this.Activity, x, ToastLength.Short).Show())
                    .DisposeWith(disposable);

                var adapter = new ReactiveListAdapter<PlaylistEntryViewModel>(this.ViewModel.Entries, (vm, parent) => new PlaylistEntryView(this.Activity, vm, parent));
                this.Playlist.Adapter = adapter;

                this.Playlist.Events().ItemClick.Select(x => x.Position)
                    .Subscribe(x =>
                    {
                        this.ViewModel.SelectedEntry = this.ViewModel.Entries[x];

                        bool hasVotesLeft = this.ViewModel.RemainingVotes > 0;
                        string voteString = hasVotesLeft ?
                            string.Format(Resources.GetString(Resource.String.votes_and_votes_left),
                                Resources.GetString(Resource.String.vote).ToQuantity(this.ViewModel.RemainingVotes.Value)) :
                            Resources.GetString(Resource.String.no_votes_left);

                        if (this.ViewModel.CanModify)
                        {
                            var builder = new AlertDialog.Builder(this.Activity);
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

                            builder.SetItems(items.ToArray(), (o, eventArgs) =>
                            {
                                switch (eventArgs.Which)
                                {
                                    case 0:
                                        this.ViewModel.PlayPlaylistSongCommand.Execute(null);
                                        break;

                                    case 1:
                                        this.ViewModel.RemoveSongCommand.Execute(null);
                                        break;

                                    case 2:
                                        this.ViewModel.MoveSongUpCommand.Execute(null);
                                        break;

                                    case 3:
                                        this.ViewModel.MoveSongDownCommand.Execute(null);
                                        break;

                                    case 4:
                                        if (hasVotesLeft)
                                        {
                                            this.ViewModel.VoteCommand.Execute(null);
                                        }
                                        break;
                                }
                            });
                            builder.Create().Show();
                        }

                        else if (this.ViewModel.CanVoteOnSelectedEntry)
                        {
                            var builder = new AlertDialog.Builder(this.Activity);
                            builder.SetTitle(Resource.String.guest_functions);

                            builder.SetItems(new[] { voteString }, (sender, args) =>
                            {
                                if (hasVotesLeft)
                                {
                                    this.ViewModel.VoteCommand.Execute(null);
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

                Func<bool, int> alphaSelector = x => x ? 255 : 100;

                // If we're setting a different background and alpha for the same button, we have to
                // do this when either of those change, or else there is a race condition where the
                // alpha is set before the background resource is changed, and therefore discarded
                this.ViewModel.WhenAnyValue(x => x.IsPlaying).Select(x => x ? Resource.Drawable.Pause : Resource.Drawable.Play)
                    .CombineLatest(this.ViewModel.PlayPauseCommand.CanExecuteObservable.Select(alphaSelector), Tuple.Create)
                    .Subscribe(x =>
                    {
                        this.PlayPauseButton.SetBackgroundResource(x.Item1);
                        this.PlayPauseButton.Background.SetAlpha(x.Item2);
                    })
                    .DisposeWith(disposable);

                this.ViewModel.PlayPreviousSongCommand.CanExecuteObservable.Select(alphaSelector)
                    .Subscribe(x => this.PreviousButton.Background.SetAlpha(x))
                    .DisposeWith(disposable);

                this.ViewModel.PlayNextSongCommand.CanExecuteObservable.Select(alphaSelector)
                    .Subscribe(x => this.NextButton.Background.SetAlpha(x))
                    .DisposeWith(disposable);

                this.ViewModel.LoadPlaylistCommand.IsExecuting
                    .Subscribe(x => this.ProgressSpinner.Visibility = x ? ViewStates.Visible : ViewStates.Gone)
                    .DisposeWith(disposable);

                this.ViewModel.LoadPlaylistCommand.ExecuteAsync()
                    .SwallowNetworkExceptions()
                    .Subscribe(_ => this.Playlist.EmptyView = this.View.FindViewById(global::Android.Resource.Id.Empty))
                    .DisposeWith(disposable);

                this.ViewModel.WhenAnyValue(x => x.CanModify).CombineLatest(this.menu, Tuple.Create)
                    .Subscribe(x => x.Item2.FindItem(Resource.Id.ToggleVideoPlayer).SetVisible(x.Item1))
                    .DisposeWith(disposable);

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

        public ProgressBar ProgressSpinner { get; private set; }

        public TextView TotalTimeTextView { get; private set; }

        public override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetHasOptionsMenu(true);

            this.ViewModel = new PlaylistViewModel();
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.PlaylistMenu, menu);

            this.menu.OnNext(menu);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.Playlist, null);

            this.WireUpControls(view);

            return view;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.ToggleVideoPlayer:
                    this.ViewModel.ToggleVideoPlayerCommand.Execute(null);
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnResume()
        {
            base.OnResume();

            this.Activity.SetTitle(Resource.String.playlist_fragment_title);
        }
    }
}