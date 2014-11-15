using Espera.Mobile.Core.Network;
using Espera.Network;
using ReactiveMarrow;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;

namespace Espera.Mobile.Core.ViewModels
{
    public class PlaylistViewModel : ReactiveObject, ISupportsActivation
    {
        public static readonly TimeSpan LoadPlaylistCommandTimeout = TimeSpan.FromSeconds(15);
        public static readonly int TimeThrottleCount = 10;
        public static readonly TimeSpan TimeThrottleDuration = TimeSpan.FromMilliseconds(100);
        private readonly Subject<int> currentTimeSecondsUserChanged;
        private readonly ReactiveList<PlaylistEntryViewModel> entries;
        private ObservableAsPropertyHelper<bool> canModify;
        private ObservableAsPropertyHelper<bool> canVoteOnSelectedEntry;
        private ObservableAsPropertyHelper<PlaylistEntryViewModel> currentSong;
        private ObservableAsPropertyHelper<int> currentTimeSeconds;
        private ObservableAsPropertyHelper<bool> isPlaying;
        private ObservableAsPropertyHelper<NetworkPlaybackState> playbackState;
        private ObservableAsPropertyHelper<int?> remainingVotes;
        private PlaylistEntryViewModel selectedEntry;
        private ObservableAsPropertyHelper<TimeSpan> totalTime;

        public PlaylistViewModel()
        {
            this.Activator = new ViewModelActivator();
            this.entries = new ReactiveList<PlaylistEntryViewModel>();
            this.currentTimeSecondsUserChanged = new Subject<int>();

            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                this.canModify = NetworkMessenger.Instance.WhenAnyValue(x => x.AccessPermission)
                    .Select(x => x == NetworkAccessPermission.Admin)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .ToProperty(this, x => x.CanModify);
                this.canModify.DisposeWith(disposable);

                this.LoadPlaylistCommand = ReactiveCommand.CreateAsyncObservable(_ =>
                    NetworkMessenger.Instance.GetCurrentPlaylistAsync().ToObservable()
                        .Timeout(LoadPlaylistCommandTimeout, RxApp.TaskpoolScheduler)
                        .TakeUntil(disposable));

                var currentPlaylist = this.LoadPlaylistCommand
                    .FirstAsync()
                    .Concat(NetworkMessenger.Instance.PlaylistChanged.ObserveOn(RxApp.MainThreadScheduler))
                    .Publish();
                currentPlaylist.Connect().DisposeWith(disposable);

                currentPlaylist.Select(x => x.Songs.Select((song, i) =>
                        new PlaylistEntryViewModel(song, x.CurrentIndex < i, x.CurrentIndex.HasValue && i == x.CurrentIndex)).ToList())
                    .Subscribe(x =>
                    {
                        using (this.entries.SuppressChangeNotifications())
                        {
                            this.entries.Clear();
                            this.entries.AddRange(x);
                        }
                    });

                this.currentSong = this.entries.Changed.Select(x => this.entries.FirstOrDefault(y => y.IsPlaying))
                    .ToProperty(this, x => x.CurrentSong);

                this.remainingVotes = NetworkMessenger.Instance.WhenAnyValue(x => x.GuestSystemInfo)
                    .Select(x => x.IsEnabled ? new int?(x.RemainingVotes) : null)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .ToProperty(this, x => x.RemainingVotes)
                    .DisposeWith(disposable);
                int? remainingVotesConnector = this.RemainingVotes;

                this.playbackState = currentPlaylist.Select(x => x.PlaybackState)
                    .Merge(NetworkMessenger.Instance.PlaybackStateChanged.ObserveOn(RxApp.MainThreadScheduler))
                    .ToProperty(this, x => x.PlaybackState)
                    .DisposeWith(disposable);

                this.currentTimeSeconds = currentPlaylist.Select(x => x.CurrentTime)
                    .Merge(NetworkMessenger.Instance.PlaybackTimeChanged)
                    .Select(x => (int)x.TotalSeconds)
                    .Select(x => Observable.Interval(TimeSpan.FromSeconds(1), RxApp.TaskpoolScheduler)
                        .Where(_ => this.IsPlaying)
                        .StartWith(x)
                        .Select((_, i) => x + i))
                    .Switch()
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .ToProperty(this, x => x.CurrentTimeSeconds)
                    .DisposeWith(disposable);

                this.currentTimeSecondsUserChanged
                    .Window(TimeThrottleDuration, TimeThrottleCount, RxApp.TaskpoolScheduler)
                    .Select(x => x.DistinctUntilChanged())
                    .Select(x => x.Take(1).Concat(x.Skip(1).TakeLast(1)))
                    .Switch()
                    .Select(x => TimeSpan.FromSeconds(x))
                    .SelectMany(x => NetworkMessenger.Instance.SetCurrentTime(x).ToObservable().SwallowNetworkExceptions())
                    .Subscribe()
                    .DisposeWith(disposable);

                this.totalTime = currentPlaylist.Select(x => x.TotalTime)
                    .ToProperty(this, x => x.TotalTime);

                var canVote = this.WhenAnyValue(x => x.CurrentSong, x => x.RemainingVotes, (currentSong, remainingVotes) =>
                        currentSong != null && remainingVotes > 0);
                this.VoteCommand = ReactiveCommand.CreateAsyncObservable(canVote, _ =>
                    NetworkMessenger.Instance.VoteAsync(this.SelectedEntry.Guid).ToObservable().TakeUntil(disposable));

                this.canVoteOnSelectedEntry = this.WhenAnyValue(x => x.SelectedEntry).Select(x => x != null && x.IsVoteAble)
                    .CombineLatest(this.WhenAnyValue(x => x.RemainingVotes).Select(x => x.HasValue), (isVoteable, hasVotes) => isVoteable && hasVotes)
                    .ToProperty(this, x => x.CanVoteOnSelectedEntry);
                var canVoteTemp = this.CanVoteOnSelectedEntry;

                this.PlayPlaylistSongCommand = ReactiveCommand.CreateAsyncObservable(this.WhenAnyValue(x => x.CanModify), _ =>
                    NetworkMessenger.Instance.PlayPlaylistSongAsync(this.SelectedEntry.Guid).ToObservable().TakeUntil(disposable));

                var canPlayNextSong = this.entries.Changed.Select(_ => this.entries)
                    .StartWith(this.entries)
                    .Select(x => x.Any(y => y.IsPlaying) && x.FirstOrDefault(y => y.IsPlaying) != x.LastOrDefault())
                    .CombineLatest(this.WhenAnyValue(x => x.CanModify), (canPlayNext, canModify) => canPlayNext && canModify);
                this.PlayNextSongCommand = ReactiveCommand.CreateAsyncObservable(canPlayNextSong, _ =>
                    NetworkMessenger.Instance.PlayNextSongAsync().ToObservable().TakeUntil(disposable));

                var canPlayPreviousSong = this.entries.Changed.Select(_ => this.entries)
                    .StartWith(this.entries)
                    .Select(x => x.Any(y => y.IsPlaying) && x.FirstOrDefault(y => y.IsPlaying) != x.FirstOrDefault())
                    .CombineLatest(this.WhenAnyValue(x => x.CanModify), (canPlayPrevious, canModify) => canPlayPrevious && canModify);
                this.PlayPreviousSongCommand = ReactiveCommand.CreateAsyncObservable(canPlayPreviousSong, _ =>
                    NetworkMessenger.Instance.PlayPreviousSongAsync().ToObservable().TakeUntil(disposable));

                this.isPlaying = this.WhenAnyValue(x => x.PlaybackState)
                    .Select(x => x == NetworkPlaybackState.Playing)
                    .ToProperty(this, x => x.IsPlaying);

                var canPlayOrPause = this.WhenAnyValue(x => x.PlaybackState)
                    .Select(x => x == NetworkPlaybackState.Playing || x == NetworkPlaybackState.Paused)
                    .CombineLatest(this.WhenAnyValue(x => x.CanModify), (canPlay, canModify) => canPlay && canModify);
                this.PlayPauseCommand = ReactiveCommand.CreateAsyncObservable(canPlayOrPause, _ =>
                {
                    if (this.IsPlaying)
                    {
                        return NetworkMessenger.Instance.PauseSongAsync().ToObservable().TakeUntil(disposable);
                    }

                    return NetworkMessenger.Instance.ContinueSongAsync().ToObservable().TakeUntil(disposable);
                });

                this.RemoveSongCommand = ReactiveCommand.CreateAsyncObservable(this.WhenAnyValue(x => x.CanModify), _ =>
                    NetworkMessenger.Instance.RemovePlaylistSongAsync(this.SelectedEntry.Guid).ToObservable().TakeUntil(disposable));

                this.MoveSongDownCommand = ReactiveCommand.CreateAsyncObservable(this.WhenAnyValue(x => x.CanModify), _ =>
                    NetworkMessenger.Instance.MovePlaylistSongDownAsync(this.SelectedEntry.Guid).ToObservable().TakeUntil(disposable));

                this.MoveSongUpCommand = ReactiveCommand.CreateAsyncObservable(this.WhenAnyValue(x => x.CanModify), _ =>
                    NetworkMessenger.Instance.MovePlaylistSongUpAsync(this.SelectedEntry.Guid).ToObservable().TakeUntil(disposable));

                this.ToggleVideoPlayerCommand = ReactiveCommand.CreateAsyncObservable(this.WhenAnyValue(x => x.CanModify), _ =>
                    NetworkMessenger.Instance.ToggleVideoPlayer().ToObservable().TakeUntil(disposable));

                return disposable;
            });
        }

        public ViewModelActivator Activator { get; }

        /// <summary>
        /// Returns whether the playlist can be modified by the users. Always true for administrators.
        /// </summary>
        public bool CanModify
        {
            get { return this.canModify.Value; }
        }

        public bool CanVoteOnSelectedEntry
        {
            get { return this.canVoteOnSelectedEntry.Value; }
        }

        public PlaylistEntryViewModel CurrentSong
        {
            get { return this.currentSong.Value; }
        }

        public int CurrentTimeSeconds
        {
            get { return this.currentTimeSeconds.Value; }
            set { this.currentTimeSecondsUserChanged.OnNext(value); }
        }

        public IReadOnlyReactiveList<PlaylistEntryViewModel> Entries
        {
            get { return this.entries; }
        }

        public bool IsPlaying
        {
            get { return this.isPlaying.Value; }
        }

        public ReactiveCommand<NetworkPlaylist> LoadPlaylistCommand { get; private set; }

        public ReactiveCommand<Unit> MoveSongDownCommand { get; private set; }

        public ReactiveCommand<Unit> MoveSongUpCommand { get; private set; }

        public NetworkPlaybackState PlaybackState
        {
            get { return this.playbackState.Value; }
        }

        public ReactiveCommand<Unit> PlayNextSongCommand { get; private set; }

        public ReactiveCommand<Unit> PlayPauseCommand { get; private set; }

        public ReactiveCommand<Unit> PlayPlaylistSongCommand { get; private set; }

        public ReactiveCommand<Unit> PlayPreviousSongCommand { get; private set; }

        public int? RemainingVotes
        {
            get { return this.remainingVotes.Value; }
        }

        public ReactiveCommand<Unit> RemoveSongCommand { get; private set; }

        public PlaylistEntryViewModel SelectedEntry
        {
            get { return this.selectedEntry; }
            set { this.RaiseAndSetIfChanged(ref this.selectedEntry, value); }
        }

        public ReactiveCommand<Unit> ToggleVideoPlayerCommand { get; private set; }

        public TimeSpan TotalTime
        {
            get { return this.totalTime.Value; }
        }

        public ReactiveCommand<Unit> VoteCommand { get; private set; }
    }
}