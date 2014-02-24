using Espera.Mobile.Core.Network;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;

namespace Espera.Mobile.Core.ViewModels
{
    public class PlaylistViewModel : ReactiveObject
    {
        private readonly BehaviorSubject<int?> currentIndex;
        private readonly ReactiveList<PlaylistEntryViewModel> entries;
        private readonly ObservableAsPropertyHelper<bool> isPlaying;
        private readonly BehaviorSubject<int?> remainingVotes;

        public PlaylistViewModel()
        {
            this.entries = new ReactiveList<PlaylistEntryViewModel>();
            this.currentIndex = new BehaviorSubject<int?>(null);
            this.remainingVotes = new BehaviorSubject<int?>(null);

            this.CanModify = NetworkMessenger.Instance.AccessPermission
                .Select(x => x == AccessPermission.Admin);

            this.LoadPlaylistCommand = new ReactiveCommand();
            this.LoadPlaylistCommand.RegisterAsync(x =>
                    NetworkMessenger.Instance.GetCurrentPlaylistAsync().ToObservable().Timeout(TimeSpan.FromSeconds(15), RxApp.TaskpoolScheduler))
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
                });

            this.VoteCommand = new ReactiveCommand();
            this.VoteCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.VoteAsync(this.entries[(int)x].Guid))
                .Subscribe();

            NetworkMessenger.Instance.RemainingVotesChanged.Subscribe(x => this.remainingVotes.OnNext(x));

            this.PlayPlaylistSongCommand = new ReactiveCommand(this.CanModify);
            this.Message = this.PlayPlaylistSongCommand.RegisterAsyncTask(x => NetworkMessenger.Instance
                    .PlayPlaylistSongAsync(this.entries[(int)x].Guid))
                    .Publish().PermaRef()
                .Select(x => x.StatusCode == 200 ? "Playing song" : "Playback failed")
                .Merge(this.LoadPlaylistCommand.ThrownExceptions.Select(_ => "Loading playlist failed")
                .Merge(this.VoteCommand.ThrownExceptions.Select(_ => "Vote failed")));

            this.PlayNextSongCommand = this.entries.Changed.Select(_ => this.entries)
                .Select(x => x.Any(y => y.IsPlaying) && x.FirstOrDefault(y => y.IsPlaying) != x.LastOrDefault())
                .CombineLatest(this.CanModify, (canPlayNext, canModify) => canPlayNext && canModify)
                .ToCommand();
            this.PlayNextSongCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.PlayNextSongAsync())
                .Subscribe();

            this.PlayPreviousSongCommand = this.entries.Changed.Select(_ => this.entries)
                .Select(x => x.Any(y => y.IsPlaying) && x.FirstOrDefault(y => y.IsPlaying) != x.FirstOrDefault())
                .CombineLatest(this.CanModify, (canPlayPrevious, canModify) => canPlayPrevious && canModify)
                .ToCommand();
            this.PlayPreviousSongCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.PlayPreviousSongAsync())
                .Subscribe();

            var playbackState = NetworkMessenger.Instance.PlaybackStateChanged
                .Merge(NetworkMessenger.Instance.GetPlaybackStateAsync().ToObservable().FirstAsync())
                .Publish(PlaybackState.None);
            playbackState.Connect();

            this.isPlaying = playbackState.Select(x => x == PlaybackState.Playing)
                .ToProperty(this, x => x.IsPlaying);

            this.PlayPauseCommand = playbackState
                .Select(x => x == PlaybackState.Playing || x == PlaybackState.Paused)
                .CombineLatest(this.CanModify, (canPlay, canModify) => canPlay && canModify)
                .ToCommand();
            this.PlayPauseCommand.Subscribe(async x =>
            {
                if (this.IsPlaying)
                {
                    await NetworkMessenger.Instance.PauseSongAsync();
                }

                else
                {
                    await NetworkMessenger.Instance.ContinueSongAsync();
                }
            });

            this.RemoveSongCommand = new ReactiveCommand(this.CanModify);
            this.RemoveSongCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.RemovePlaylistSongAsync(this.entries[(int)x].Guid))
                .Subscribe();

            this.MoveSongDownCommand = this.CanModify.ToCommand();
            this.MoveSongDownCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.MovePlaylistSongDownAsync(this.entries[(int)x].Guid))
                .Subscribe();

            this.MoveSongUpCommand = this.CanModify.ToCommand();
            this.MoveSongUpCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.MovePlaylistSongUpAsync(this.entries[(int)x].Guid))
                .Subscribe();
        }

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

        public ReactiveCommand LoadPlaylistCommand { get; private set; }

        public IObservable<string> Message { get; private set; }

        public ReactiveCommand MoveSongDownCommand { get; private set; }

        public ReactiveCommand MoveSongUpCommand { get; private set; }

        public ReactiveCommand PlayNextSongCommand { get; private set; }

        public ReactiveCommand PlayPauseCommand { get; private set; }

        public ReactiveCommand PlayPlaylistSongCommand { get; private set; }

        public ReactiveCommand PlayPreviousSongCommand { get; private set; }

        public IObservable<int?> RemainingVotes
        {
            get { return this.remainingVotes.AsObservable(); }
        }

        public ReactiveCommand RemoveSongCommand { get; private set; }

        public ReactiveCommand VoteCommand { get; private set; }
    }
}