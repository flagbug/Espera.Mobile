using ReactiveUI;
using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace Espera.Android
{
    public class PlaylistViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<bool> isPlaying;
        private readonly ObservableAsPropertyHelper<Playlist> playlist;

        public PlaylistViewModel()
        {
            this.LoadPlaylistCommand = new ReactiveCommand();
            this.playlist = this.LoadPlaylistCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.GetCurrentPlaylist())
                .ToProperty(this, x => x.Playlist);

            NetworkMessenger.Instance.PlaylistChanged.Where(_ => this.Playlist != null)
                .Subscribe(x =>
                {
                    this.Playlist.Songs = x.Songs;
                    this.Playlist.CurrentIndex = x.CurrentIndex;
                });

            NetworkMessenger.Instance.PlaylistIndexChanged.Where(_ => this.Playlist != null)
                .Subscribe(x => this.Playlist.CurrentIndex = x);

            this.PlayPlaylistSongCommand = new ReactiveCommand();
            this.Message = this.PlayPlaylistSongCommand.RegisterAsyncTask(x => NetworkMessenger.Instance
                    .PlayPlaylistSong(this.Playlist.Songs[(int)x].Guid))
                .Select(x => x.Item1 == 200 ? "Playing song" : "Playback failed");

            this.PlayNextSongCommand = this.playlist.Where(x => x != null)
                .Select(x => x.Changed.Select(y => x).StartWith(x))
                .Switch()
                .Select(x => x.CurrentIndex != null && x.CurrentIndex < x.Songs.Count - 1)
                .ToCommand();
            this.PlayNextSongCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.PlayNextSong());

            this.PlayPreviousSongCommand = this.playlist.Where(x => x != null)
                .Select(x => x.Changed.Select(y => x).StartWith(x))
                .Switch()
                .Select(x => x.CurrentIndex != null && x.CurrentIndex > 0)
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

            this.RemoveSongCommand = new ReactiveCommand();
            this.RemoveSongCommand.RegisterAsyncTask(x =>
                NetworkMessenger.Instance.RemovePlaylistSong(this.Playlist.Songs[(int)x].Guid))
                .Where(x => x.Item1 == 200)
                .InvokeCommand(this.LoadPlaylistCommand); // The server doesn't send an update...no idea why
        }

        public bool IsPlaying
        {
            get { return this.isPlaying.Value; }
        }

        public ReactiveCommand LoadPlaylistCommand { get; private set; }

        public IObservable<string> Message { get; private set; }

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