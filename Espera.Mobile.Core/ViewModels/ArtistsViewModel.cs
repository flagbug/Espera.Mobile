using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Akavache;
using Espera.Mobile.Core.Network;
using Espera.Network;
using Newtonsoft.Json;
using ReactiveUI;

namespace Espera.Mobile.Core.ViewModels
{
    public class ArtistsViewModel : ReactiveObject, ISupportsActivation
    {
        private ObservableAsPropertyHelper<IReadOnlyList<string>> artists;
        private IReadOnlyList<NetworkSong> songs;

        public ArtistsViewModel()
        {
            this.Activator = new ViewModelActivator();

            this.WhenActivated(d =>
            {
                this.LoadCommand = ReactiveCommand.Create(_ => GetSongsAsync());
                this.artists = this.LoadCommand
                   .Do(x => this.songs = x)
                   .Select(GetArtists)
                   .ToProperty(this, x => x.Artists, new List<string>());

                this.Messages = this.LoadCommand.ThrownExceptions.Select(_ => "Loading artists failed");
            });
        }

        public ViewModelActivator Activator { get; private set; }

        public IReadOnlyList<string> Artists
        {
            get { return this.artists.Value; }
        }

        public ReactiveCommand<IReadOnlyList<NetworkSong>> LoadCommand { get; private set; }

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

        private IObservable<IReadOnlyList<NetworkSong>> GetSongsAsync()
        {
			return songs == null ? NetworkMessenger.Instance.GetSongsAsync().ToObservable()
				.Timeout(TimeSpan.FromSeconds(15), RxApp.TaskpoolScheduler) : Observable.Return(this.songs);
        }
    }
}