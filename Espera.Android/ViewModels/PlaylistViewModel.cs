using Espera.Android.Network;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace Espera.Android.ViewModels
{
    public class PlaylistViewModel : ReactiveObject
    {
        private readonly ReactiveList<PlaylistEntryViewModel> entries;
        private readonly ObservableAsPropertyHelper<bool> isPlaying;

        public PlaylistViewModel()
        {
            this.entries = new ReactiveList<PlaylistEntryViewModel>();

            this.CanModify = NetworkMessenger.Instance.AccessPermission
                .Select(x => x == AccessPermission.Admin);

            this.LoadPlaylistCommand = new ReactiveCommand();

            this.LoadPlaylistCommand.RegisterAsync(x =>
                    NetworkMessenger.Instance.GetCurrentPlaylist().ToObservable().Timeout(TimeSpan.FromSeconds(15), RxApp.TaskpoolScheduler))
                .Merge(NetworkMessenger.Instance.PlaylistChanged)
                .Select(x => x.Songs.Select((song, i) => new PlaylistEntryViewModel(song, x.CurrentIndex.HasValue && i == x.CurrentIndex)))
                .Subscribe(x =>
                {
                    this.entries.Clear();
                    this.entries.AddRange(x);
                });

            this.PlayPlaylistSongCommand = new ReactiveCommand(this.CanModify);
            this.Message = this.PlayPlaylistSongCommand.RegisterAsyncTask(x => NetworkMessenger.Instance
                    .PlayPlaylistSong(this.entries[(int)x].Guid))
                .Select(x => x.StatusCode == 200 ? "Playing song" : "Playback failed")
                .Merge(this.LoadPlaylistCommand.ThrownExceptions.Select(_ => "Loading playlist failed"));

            this.PlayNextSongCommand = this.entries.Changed.Select(_ => this.entries)
                .Select(x => x.Any(y => y.IsPlaying) && x.FirstOrDefault(y => y.IsPlaying) != x.LastOrDefault())
                .CombineLatest(this.CanModify, (canPlayNext, canModify) => canPlayNext && canModify)
                .ToCommand();
            this.PlayNextSongCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.PlayNextSong());

            this.PlayPreviousSongCommand = this.entries.Changed.Select(_ => this.entries)
                .Select(x => x.Any(y => y.IsPlaying) && x.FirstOrDefault(y => y.IsPlaying) != x.FirstOrDefault())
                .CombineLatest(this.CanModify, (canPlayPrevious, canModify) => canPlayPrevious && canModify)
                .ToCommand();
            this.PlayPreviousSongCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.PlayPreviousSong());

            var playbackState = NetworkMessenger.Instance.PlaybackStateChanged
                .Merge(NetworkMessenger.Instance.GetPlaybackState().ToObservable().FirstAsync())
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
                    await NetworkMessenger.Instance.PauseSong();
                }

                else
                {
                    await NetworkMessenger.Instance.ContinueSong();
                }
            });

            this.RemoveSongCommand = new ReactiveCommand(this.CanModify);
            this.RemoveSongCommand.RegisterAsyncTask(x =>
                NetworkMessenger.Instance.RemovePlaylistSong(this.entries[(int)x].Guid))
                .Where(x => x.StatusCode == 200)
                .InvokeCommand(this.LoadPlaylistCommand); // The server doesn't send an update...no idea why

            this.MoveSongDownCommand = this.CanModify.ToCommand();
            this.MoveSongDownCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.MovePlaylistSongDown(this.entries[(int)x].Guid));

            this.MoveSongUpCommand = this.CanModify.ToCommand();
            this.MoveSongUpCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.MovePlaylistSongUp(this.entries[(int)x].Guid));

            this.VoteCommand = new ReactiveCommand();
            this.VoteCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.Vote(this.entries[(int)x].Guid));
        }

        public IObservable<bool> CanModify { get; private set; }

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

        public ReactiveCommand RemoveSongCommand { get; private set; }

        public ReactiveCommand VoteCommand { get; private set; }
    }
}