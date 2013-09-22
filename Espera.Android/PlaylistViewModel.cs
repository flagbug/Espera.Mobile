using ReactiveUI;
using System;
using System.Reactive.Linq;

namespace Espera.Android
{
    public class PlaylistViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<Playlist> playlist;

        public PlaylistViewModel()
        {
            this.LoadPlaylistCommand = new ReactiveCommand();
            this.playlist = this.LoadPlaylistCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.GetCurrentPlaylist())
                .Merge(NetworkMessenger.Instance.PlaylistChanged)
                .ToProperty(this, x => x.Playlist);

            this.PlayPlaylistSongCommand = new ReactiveCommand();
            this.Message = this.PlayPlaylistSongCommand.RegisterAsyncTask(x => NetworkMessenger.Instance
                    .PlayPlaylistSong(this.Playlist.Songs[(int)x].Guid))
                .Select(x => x.Item1 == 200 ? "Playing song" : "Playback failed");
        }

        public ReactiveCommand LoadPlaylistCommand { get; private set; }

        public IObservable<string> Message { get; private set; }

        public Playlist Playlist
        {
            get { return this.playlist.Value; }
        }

        public ReactiveCommand PlayPlaylistSongCommand { get; private set; }
    }
}