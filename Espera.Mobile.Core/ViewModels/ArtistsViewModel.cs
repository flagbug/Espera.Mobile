using Espera.Mobile.Core.SongFetchers;
using Espera.Network;
using Newtonsoft.Json;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Espera.Mobile.Core.ViewModels
{
    public class ArtistsViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<IReadOnlyList<string>> artists;
        private IReadOnlyList<NetworkSong> songs;

        public ArtistsViewModel(ISongFetcher songFetcher)
        {
            if (songFetcher == null)
                throw new ArgumentNullException("songFetcher");

            this.LoadCommand = new ReactiveCommand();
            this.artists = this.LoadCommand.RegisterAsync(x => songFetcher.GetSongsAsync())
               .Do(x => this.songs = x)
               .Select(GetArtists)
               .ToProperty(this, x => x.Artists, new List<string>());

            this.Messages = this.LoadCommand.ThrownExceptions.Select(_ => "Loading artists failed");
        }

        public IReadOnlyList<string> Artists
        {
            get { return this.artists.Value; }
        }

        public ReactiveCommand LoadCommand { get; private set; }

        public IObservable<string> Messages { get; private set; }

        public string SerializeSongsForSelectedArtist(string artist)
        {
            IReadOnlyList<NetworkSong> filteredSongs = this.songs
                .Where(x => x.Artist.Equals(artist, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            return JsonConvert.SerializeObject(filteredSongs, Formatting.None);
        }

        private static IReadOnlyList<string> GetArtists(IEnumerable<NetworkSong> songs)
        {
            return songs.GroupBy(s => s.Artist)
                .Select(g => g.Key)
                .Distinct(StringComparer.InvariantCultureIgnoreCase)
                .OrderBy(_ => _)
                .ToList();
        }
    }
}