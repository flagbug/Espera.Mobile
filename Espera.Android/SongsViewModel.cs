using Akavache;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Espera.Android
{
    public class SongsViewModel : ReactiveObject
    {
        private readonly string artist;

        private readonly ObservableAsPropertyHelper<IReadOnlyList<Song>> songs;

        public SongsViewModel(string artist)
        {
            if (artist == null)
                throw new ArgumentNullException("artist");

            this.artist = artist;

            this.LoadArtistsCommand = new ReactiveCommand();
            this.songs = this.LoadArtistsCommand.RegisterAsync(x => BlobCache.InMemory.GetObjectAsync<IReadOnlyList<Song>>("songs"))
                .Select(x => x.Where(y => y.Artist.Equals(this.artist, StringComparison.OrdinalIgnoreCase)).ToList())
                .ToProperty(this, x => x.Songs, new List<Song>());

            this.PlaySongsCommand = new ReactiveCommand();
            var playSongsMessage = this.PlaySongsCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.PlaySongs(this.Songs.Skip((int)x).Select(y => y.Guid)))
                .Select(x => x.Item1 == 200 ? "Playing songs" : "Error adding songs");

            this.AddToPlaylistCommand = new ReactiveCommand();
            var addToPlaylistMessage = this.AddToPlaylistCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.AddSongToPlaylist(this.Songs[(int)x].Guid))
                .Select(x => x.Item1 == 200 ? "Song added to playlist" : "Error adding song");

            this.Message = playSongsMessage.Merge(addToPlaylistMessage).Throttle(TimeSpan.FromMilliseconds(200));
        }

        public ReactiveCommand AddToPlaylistCommand { get; private set; }

        public ReactiveCommand LoadArtistsCommand { get; private set; }

        public IObservable<string> Message { get; private set; }

        public ReactiveCommand PlaySongsCommand { get; private set; }

        public IReadOnlyList<Song> Songs
        {
            get { return this.songs.Value; }
        }
    }
}