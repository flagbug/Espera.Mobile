using Espera.Android.Network;
using ReactiveUI;
using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace Espera.Android.ViewModels
{
    public class PlaylistViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<bool> isPlaying;
        private readonly ObservableAsPropertyHelper<Playlist> playlist;

        public PlaylistViewModel()
        {
            this.CanModify = NetworkMessenger.Instance.AccessPermission
                .Select(x => x == AccessPermission.Admin);

            this.LoadPlaylistCommand = new ReactiveCommand();
            this.playlist = this.LoadPlaylistCommand.RegisterAsync(x =>
                    NetworkMessenger.Instance.GetCurrentPlaylist().ToObservable().Timeout(TimeSpan.FromSeconds(15), RxApp.TaskpoolScheduler))
                .ToProperty(this, x => x.Playlist);

            NetworkMessenger.Instance.PlaylistChanged.Where(_ => this.Playlist != null)
                .Subscribe(x =>
                {
                    this.Playlist.Songs = x.Songs;
                    this.Playlist.CurrentIndex = x.CurrentIndex;
                });

            NetworkMessenger.Instance.PlaylistIndexChanged.Where(_ => this.Playlist != null)
                .Subscribe(x => this.Playlist.CurrentIndex = x);

            this.PlayPlaylistSongCommand = new ReactiveCommand(this.CanModify);
            this.Message = this.PlayPlaylistSongCommand.RegisterAsyncTask(x => NetworkMessenger.Instance
                    .PlayPlaylistSong(this.Playlist.Songs[(int)x].Guid))
                .Select(x => x.StatusCode == 200 ? "Playing song" : "Playback failed")
                .Merge(this.LoadPlaylistCommand.ThrownExceptions.Select(_ => "Loading playlist failed"));

            this.PlayNextSongCommand = this.playlist.Where(x => x != null)
                .Select(x => x.Changed.Select(y => x).StartWith(x))
                .Switch()
                .Select(x => x.CurrentIndex != null && x.CurrentIndex < x.Songs.Count - 1)
                .CombineLatest(this.CanModify, (canPlayNext, canModify) => canPlayNext && canModify)
                .ToCommand();
            this.PlayNextSongCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.PlayNextSong());

            this.PlayPreviousSongCommand = this.playlist.Where(x => x != null)
                .Select(x => x.Changed.Select(y => x).StartWith(x))
                .Switch()
                .Select(x => x.CurrentIndex != null && x.CurrentIndex > 0)
                .CombineLatest(this.CanModify, (canPlayPrevious, canModify) => canPlayPrevious && canModify)
                .ToCommand();
            this.PlayPreviousSongCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.PlayPreviousSong());

            var playbackState = NetworkMessenger.Instance.PlaybackStateChanged
                .Merge(NetworkMessenger.Instance.GetPlaybackSate().ToObservable().FirstAsync())
                .Publish(PlaybackState.None);
            playbackState.Connect();

            this.isPlaying = playbackState
                .Select(x => x == PlaybackState.Playing)
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
                NetworkMessenger.Instance.RemovePlaylistSong(this.Playlist.Songs[(int)x].Guid))
                .Where(x => x.StatusCode == 200)
                .InvokeCommand(this.LoadPlaylistCommand); // The server doesn't send an update...no idea why

            this.MoveSongDownCommand = this.CanModify.ToCommand();
            this.MoveSongDownCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.MovePlaylistSongDown(this.Playlist.Songs[(int)x].Guid));

            this.MoveSongUpCommand = this.CanModify.ToCommand();
            this.MoveSongUpCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.MovePlaylistSongUp(this.Playlist.Songs[(int)x].Guid));
        }

        public IObservable<bool> CanModify { get; private set; }

        public bool IsPlaying
        {
            get { return this.isPlaying.Value; }
        }

        public ReactiveCommand LoadPlaylistCommand { get; private set; }

        public IObservable<string> Message { get; private set; }

        public ReactiveCommand MoveSongDownCommand { get; private set; }

        public ReactiveCommand MoveSongUpCommand { get; private set; }

        public Playlist Playlist
        {
            get { return this.playlist.Value; }
        }

        public ReactiveCommand PlayNextSongCommand { get; private set; }

        public ReactiveCommand PlayPauseCommand { get; private set; }

        public ReactiveCommand PlayPlaylistSongCommand { get; private set; }

        public ReactiveCommand PlayPreviousSongCommand { get; private set; }

        public ReactiveCommand RemoveSongCommand { get; private set; }
    }
}