using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Espera.Mobile.Core.SongFetchers;
using Espera.Mobile.Core.Songs;
using Newtonsoft.Json;
using ReactiveUI;

namespace Espera.Mobile.Core.ViewModels
{
    public class ArtistsViewModel<T> : ReactiveObject where T : Song
    {
        private readonly ObservableAsPropertyHelper<IReadOnlyList<string>> artists;
        private IReadOnlyList<T> songs;

        public ArtistsViewModel(ISongFetcher<T> songFetcher)
        {
            if (songFetcher == null)
                throw new ArgumentNullException("songFetcher");

            this.LoadCommand = ReactiveCommand.CreateAsyncObservable(_ => songFetcher.GetSongsAsync());
            this.artists = this.LoadCommand
               .Do(x => this.songs = x)
               .Select(GetArtists)
               .ToProperty(this, x => x.Artists, new List<string>());

            this.Messages = this.LoadCommand.ThrownExceptions.Select(_ => "Loading artists failed");
        }

        public IReadOnlyList<string> Artists
        {
            get { return this.artists.Value; }
        }

        public ReactiveCommand<IReadOnlyList<T>> LoadCommand { get; private set; }

        public IObservable<string> Messages { get; private set; }

        public string SerializeSongsForSelectedArtist(string artist)
        {
            IReadOnlyList<T> filteredSongs = this.songs
                .Where(x => x.Artist.Equals(artist, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            return JsonConvert.SerializeObject(filteredSongs, Formatting.None);
        }

        private static IReadOnlyList<string> GetArtists(IEnumerable<T> songs)
        {
            return songs.GroupBy(s => s.Artist)
                .Select(g => g.Key)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .OrderBy(_ => _)
                .ToList();
        }
    }
}