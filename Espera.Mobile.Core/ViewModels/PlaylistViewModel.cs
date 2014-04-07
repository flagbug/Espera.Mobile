using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Espera.Mobile.Core.Network;
using Espera.Network;
using ReactiveUI;

namespace Espera.Mobile.Core.ViewModels
{
    public class PlaylistViewModel : ReactiveObject, ISupportsActivation
    {
        private BehaviorSubject<int?> currentIndex;
        private ReactiveList<PlaylistEntryViewModel> entries;
        private ObservableAsPropertyHelper<bool> isPlaying;
        private BehaviorSubject<NetworkPlaybackState> playbackState;
        private BehaviorSubject<int?> remainingVotes;

        public PlaylistViewModel()
        {
            this.Activator = new ViewModelActivator();

            this.WhenActivated(() =>
            {
                this.entries = new ReactiveList<PlaylistEntryViewModel>();
                this.currentIndex = new BehaviorSubject<int?>(null);
                this.remainingVotes = new BehaviorSubject<int?>(null);
                this.playbackState = new BehaviorSubject<NetworkPlaybackState>(NetworkPlaybackState.None);
                var disposable = new CompositeDisposable();

                var canModifyConn = NetworkMessenger.Instance.AccessPermission
                    .Select(x => x == NetworkAccessPermission.Admin)
                    .Publish();
                this.CanModify = canModifyConn;

                canModifyConn.Connect().DisposeWith(disposable);

                this.LoadPlaylistCommand = ReactiveCommand.Create(_ => NetworkMessenger.Instance.GetCurrentPlaylistAsync().ToObservable()
                    .Timeout(TimeSpan.FromSeconds(15), RxApp.TaskpoolScheduler));
                this.LoadPlaylistCommand
                    .Merge(NetworkMessenger.Instance.PlaylistChanged)
                    .Select(x => Tuple.Create(x, x.Songs.Select((song, i) => new PlaylistEntryViewModel(song, x.CurrentIndex.HasValue && i == x.CurrentIndex))))
                    .Subscribe(x =>
                    {
                        using (this.entries.SuppressChangeNotifications())
                        {
                            this.entries.Clear();
                            this.entries.AddRange(x.Item2);
                        }

                        this.currentIndex.OnNext(x.Item1.CurrentIndex);
                        this.remainingVotes.OnNext(x.Item1.RemainingVotes);
                        this.playbackState.OnNext(x.Item1.PlaybackState);
                    }).DisposeWith(disposable);

                var canVote = this.CurrentIndex.CombineLatest(this.RemainingVotes, (currentIndex, remainingVotes) =>
                        currentIndex.HasValue && remainingVotes.HasValue && remainingVotes > 0);
                this.VoteCommand = ReactiveCommand.CreateAsync(canVote, x => NetworkMessenger.Instance.VoteAsync(this.entries[(int)x].Guid));

                NetworkMessenger.Instance.RemainingVotesChanged.Subscribe(x => this.remainingVotes.OnNext(x));
                NetworkMessenger.Instance.PlaybackStateChanged.Subscribe(x => this.playbackState.OnNext(x));

                this.PlayPlaylistSongCommand = ReactiveCommand.CreateAsync(this.CanModify, x => NetworkMessenger.Instance
                        .PlayPlaylistSongAsync(this.entries[(int)x].Guid));
                this.Message = this.PlayPlaylistSongCommand
                    .Select(x => x.Status == ResponseStatus.Success ? "Playing song" : "Playback failed")
                    .Merge(this.LoadPlaylistCommand.ThrownExceptions.Select(_ => "Loading playlist failed")
                    .Merge(this.VoteCommand.ThrownExceptions.Select(_ => "Vote failed")));

                var canPlayNextSong = this.entries.Changed.Select(_ => this.entries)
                    .Select(x => x.Any(y => y.IsPlaying) && x.FirstOrDefault(y => y.IsPlaying) != x.LastOrDefault())
                    .CombineLatest(this.CanModify, (canPlayNext, canModify) => canPlayNext && canModify);
                this.PlayNextSongCommand = ReactiveCommand.CreateAsync(canPlayNextSong, _ => NetworkMessenger.Instance.PlayNextSongAsync());

                var canPlayPreviousSong = this.entries.Changed.Select(_ => this.entries)
                    .Select(x => x.Any(y => y.IsPlaying) && x.FirstOrDefault(y => y.IsPlaying) != x.FirstOrDefault())
                    .CombineLatest(this.CanModify, (canPlayPrevious, canModify) => canPlayPrevious && canModify);
                this.PlayPreviousSongCommand = ReactiveCommand.CreateAsync(canPlayPreviousSong, _ => NetworkMessenger.Instance.PlayPreviousSongAsync());

                this.isPlaying = playbackState.Select(x => x == NetworkPlaybackState.Playing)
                    .ToProperty(this, x => x.IsPlaying);

                var canPlayOrPause = playbackState.Select(x => x == NetworkPlaybackState.Playing || x == NetworkPlaybackState.Paused)
                    .CombineLatest(this.CanModify, (canPlay, canModify) => canPlay && canModify);
                this.PlayPauseCommand = ReactiveCommand.CreateAsync(canPlayOrPause, _ =>
                {
                    if (this.IsPlaying)
                    {
                        return NetworkMessenger.Instance.PauseSongAsync();
                    }

                    return NetworkMessenger.Instance.ContinueSongAsync();
                });

                this.RemoveSongCommand = ReactiveCommand.CreateAsync(this.CanModify, x =>
                    NetworkMessenger.Instance.RemovePlaylistSongAsync(this.entries[(int)x].Guid));

                this.MoveSongDownCommand = ReactiveCommand.CreateAsync(this.CanModify, x =>
                    NetworkMessenger.Instance.MovePlaylistSongDownAsync(this.entries[(int)x].Guid));

                this.MoveSongUpCommand = ReactiveCommand.CreateAsync(this.CanModify, x =>
                    NetworkMessenger.Instance.MovePlaylistSongUpAsync(this.entries[(int)x].Guid));

                return disposable;
            });
        }

        public ViewModelActivator Activator { get; private set; }

        /// <summary>
        /// Returns whether the playlist can be modified by the users. Always true for administrators.
        /// </summary>
        public IObservable<bool> CanModify { get; private set; }

        /// <summary>
        /// The index of the currently playing song. Null, if no song is playing.
        /// </summary>
        public IObservable<int?> CurrentIndex
        {
            get { return this.currentIndex.AsObservable(); }
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

        public ReactiveCommand<ResponseInfo> PlayNextSongCommand { get; private set; }

        public ReactiveCommand<ResponseInfo> PlayPauseCommand { get; private set; }

        public ReactiveCommand<ResponseInfo> PlayPlaylistSongCommand { get; private set; }

        public ReactiveCommand<ResponseInfo> PlayPreviousSongCommand { get; private set; }

        public IObservable<int?> RemainingVotes
        {
            get { return this.remainingVotes.AsObservable(); }
        }

        public ReactiveCommand<ResponseInfo> RemoveSongCommand { get; private set; }

        public ReactiveCommand<ResponseInfo> VoteCommand { get; private set; }
    }
}