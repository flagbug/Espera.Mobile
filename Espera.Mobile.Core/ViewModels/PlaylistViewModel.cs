using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Espera.Mobile.Core.Network;
using Espera.Network;
using ReactiveMarrow;
using ReactiveUI;

namespace Espera.Mobile.Core.ViewModels
{
    public class PlaylistViewModel : ReactiveObject, ISupportsActivation
    {
        private ObservableAsPropertyHelper<bool> canModify;
        private ObservableAsPropertyHelper<bool> canVoteOnSelectedEntry;
        private ObservableAsPropertyHelper<PlaylistEntryViewModel> currentSong;
        private ObservableAsPropertyHelper<int> currentTimeSeconds;
        private ReactiveList<PlaylistEntryViewModel> entries;
        private ObservableAsPropertyHelper<bool> isPlaying;
        private ObservableAsPropertyHelper<NetworkPlaybackState> playbackState;
        private ObservableAsPropertyHelper<int?> remainingVotes;
        private PlaylistEntryViewModel selectedEntry;
        private ObservableAsPropertyHelper<TimeSpan> totalTime;

        public PlaylistViewModel()
        {
            this.Activator = new ViewModelActivator();
            this.entries = new ReactiveList<PlaylistEntryViewModel>();

            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                this.canModify = NetworkMessenger.Instance.AccessPermission
                    .Select(x => x == NetworkAccessPermission.Admin)
                    .ToProperty(this, x => x.CanModify);
                var temp = this.CanModify;
                this.canModify.DisposeWith(disposable);

                this.LoadPlaylistCommand = ReactiveCommand.CreateAsyncObservable(_ =>
                    NetworkMessenger.Instance.GetCurrentPlaylistAsync().ToObservable()
                        .Timeout(TimeSpan.FromSeconds(15), RxApp.TaskpoolScheduler));

                var currentPlaylist = this.LoadPlaylistCommand
                    .FirstAsync()
                    .Concat(NetworkMessenger.Instance.PlaylistChanged)
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

                this.currentSong = this.WhenAnyValue(x => x.Entries).Select(x => x.FirstOrDefault(y => y.IsPlaying))
                    .ToProperty(this, x => x.CurrentSong);

                this.remainingVotes = currentPlaylist.Select(x => x.RemainingVotes)
                    .Merge(NetworkMessenger.Instance.RemainingVotesChanged)
                    .ToProperty(this, x => x.RemainingVotes)
                    .DisposeWith(disposable);

                this.playbackState = currentPlaylist.Select(x => x.PlaybackState)
                    .Merge(NetworkMessenger.Instance.PlaybackStateChanged)
                    .ToProperty(this, x => x.PlaybackState);

                this.currentTimeSeconds = currentPlaylist.Select(x => x.CurrentTime)
                    .Merge(NetworkMessenger.Instance.PlaybackTimeChanged)
                    .Select(x => (int)x.TotalSeconds)
                    .Select(x => Observable.Interval(TimeSpan.FromSeconds(1), RxApp.TaskpoolScheduler)
                        .Select((_, i) => x + i).StartWith(x).Where(_ => this.IsPlaying))
                    .Switch()
                    .ToProperty(this, x => x.CurrentTimeSeconds);

                this.totalTime = currentPlaylist.Select(x => x.TotalTime)
                    .ToProperty(this, x => x.TotalTime, TimeSpan.FromSeconds(1));

                var canVote = this.WhenAnyValue(x => x.CurrentSong, x => x.RemainingVotes, (currentSong, remainingVotes) =>
                        currentSong != null && remainingVotes > 0);
                this.VoteCommand = ReactiveCommand.CreateAsyncTask(canVote, _ => NetworkMessenger.Instance.VoteAsync(this.SelectedEntry.Guid));

                this.canVoteOnSelectedEntry = this.WhenAnyValue(x => x.SelectedEntry).Select(x => x != null && x.IsVoteAble)
                    .CombineLatest(this.WhenAnyValue(x => x.RemainingVotes).Select(x => x.HasValue), (isVoteable, hasVotes) => isVoteable && hasVotes)
                    .ToProperty(this, x => x.CanVoteOnSelectedEntry);
                var canVoteTemp = this.CanVoteOnSelectedEntry;

                this.PlayPlaylistSongCommand = ReactiveCommand.CreateAsyncTask(this.WhenAnyValue(x => x.CanModify), _ => NetworkMessenger.Instance
                    .PlayPlaylistSongAsync(this.SelectedEntry.Guid));
                this.Message = this.PlayPlaylistSongCommand
                    .Select(x => x.Status == ResponseStatus.Success ? "Playing song" : "Playback failed")
                    .Merge(this.LoadPlaylistCommand.ThrownExceptions.Select(_ => "Loading playlist failed")
                    .Merge(this.VoteCommand.ThrownExceptions.Select(_ => "Vote failed")));

                var canPlayNextSong = this.entries.Changed.Select(_ => this.entries)
                    .StartWith(this.entries)
                    .Select(x => x.Any(y => y.IsPlaying) && x.FirstOrDefault(y => y.IsPlaying) != x.LastOrDefault())
                    .CombineLatest(this.WhenAnyValue(x => x.CanModify), (canPlayNext, canModify) => canPlayNext && canModify);
                this.PlayNextSongCommand = ReactiveCommand.CreateAsyncTask(canPlayNextSong, _ => NetworkMessenger.Instance.PlayNextSongAsync());

                var canPlayPreviousSong = this.entries.Changed.Select(_ => this.entries)
                    .StartWith(this.entries)
                    .Select(x => x.Any(y => y.IsPlaying) && x.FirstOrDefault(y => y.IsPlaying) != x.FirstOrDefault())
                    .CombineLatest(this.WhenAnyValue(x => x.CanModify), (canPlayPrevious, canModify) => canPlayPrevious && canModify);
                this.PlayPreviousSongCommand = ReactiveCommand.CreateAsyncTask(canPlayPreviousSong, _ => NetworkMessenger.Instance.PlayPreviousSongAsync());

                this.isPlaying = this.WhenAnyValue(x => x.PlaybackState)
                    .Select(x => x == NetworkPlaybackState.Playing)
                    .ToProperty(this, x => x.IsPlaying);

                var canPlayOrPause = this.WhenAnyValue(x => x.PlaybackState)
                    .Select(x => x == NetworkPlaybackState.Playing || x == NetworkPlaybackState.Paused)
                    .CombineLatest(this.WhenAnyValue(x => x.CanModify), (canPlay, canModify) => canPlay && canModify);
                this.PlayPauseCommand = ReactiveCommand.CreateAsyncTask(canPlayOrPause, _ =>
                {
                    if (this.IsPlaying)
                    {
                        return NetworkMessenger.Instance.PauseSongAsync();
                    }

                    return NetworkMessenger.Instance.ContinueSongAsync();
                });

                this.RemoveSongCommand = ReactiveCommand.CreateAsyncTask(this.WhenAnyValue(x => x.CanModify), _ =>
                    NetworkMessenger.Instance.RemovePlaylistSongAsync(this.SelectedEntry.Guid));

                this.MoveSongDownCommand = ReactiveCommand.CreateAsyncTask(this.WhenAnyValue(x => x.CanModify), _ =>
                    NetworkMessenger.Instance.MovePlaylistSongDownAsync(this.SelectedEntry.Guid));

                this.MoveSongUpCommand = ReactiveCommand.CreateAsyncTask(this.WhenAnyValue(x => x.CanModify), _ =>
                    NetworkMessenger.Instance.MovePlaylistSongUpAsync(this.SelectedEntry.Guid));

                return disposable;
            });
        }

        public ViewModelActivator Activator { get; private set; }

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
            set { NetworkMessenger.Instance.SetCurrentTime(TimeSpan.FromSeconds(value)); }
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

        public IObservable<string> Message { get; private set; }

        public ReactiveCommand<ResponseInfo> MoveSongDownCommand { get; private set; }

        public ReactiveCommand<ResponseInfo> MoveSongUpCommand { get; private set; }

        public NetworkPlaybackState PlaybackState
        {
            get { return this.playbackState.Value; }
        }

        public ReactiveCommand<ResponseInfo> PlayNextSongCommand { get; private set; }

        public ReactiveCommand<ResponseInfo> PlayPauseCommand { get; private set; }

        public ReactiveCommand<ResponseInfo> PlayPlaylistSongCommand { get; private set; }

        public ReactiveCommand<ResponseInfo> PlayPreviousSongCommand { get; private set; }

        public int? RemainingVotes
        {
            get { return this.remainingVotes.Value; }
        }

        public ReactiveCommand<ResponseInfo> RemoveSongCommand { get; private set; }

        public PlaylistEntryViewModel SelectedEntry
        {
            get { return this.selectedEntry; }
            set { this.RaiseAndSetIfChanged(ref this.selectedEntry, value); }
        }

        public TimeSpan TotalTime
        {
            get { return this.totalTime.Value; }
        }

        public ReactiveCommand<ResponseInfo> VoteCommand { get; private set; }
    }
}