using ReactiveUI;

namespace Espera.Android
{
    public class PlaylistViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<Playlist> playlist;

        public PlaylistViewModel()
        {
            this.LoadPlaylistCommand = new ReactiveCommand();
            this.playlist = this.LoadPlaylistCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.GetCurrentPlaylist())
                .ToProperty(this, x => x.Playlist);
        }

        public ReactiveCommand LoadPlaylistCommand { get; private set; }

        public Playlist Playlist
        {
            get { return this.playlist.Value; }
        }
    }
}