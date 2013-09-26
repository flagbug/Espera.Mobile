using Akavache;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Espera.Android
{
    public class ArtistsViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<IReadOnlyList<string>> artists;
        private string selectedArtist;

        public ArtistsViewModel()
        {
            this.LoadCommand = new ReactiveCommand();
            this.artists = this.LoadCommand.RegisterAsync(x => LoadSongsAsync().ToObservable().Timeout(TimeSpan.FromSeconds(15)))
               .Select(x => x.GroupBy(s => s.Artist).Select(g => g.Key).Distinct(StringComparer.InvariantCultureIgnoreCase).OrderBy(_ => _).ToList())
               .ToProperty(this, x => x.Artists, new List<string>());

            this.Messages = this.LoadCommand.ThrownExceptions.Select(_ => "Loading artists failed");
        }

        public IReadOnlyList<string> Artists
        {
            get { return this.artists.Value; }
        }

        public ReactiveCommand LoadCommand { get; private set; }

        public IObservable<string> Messages { get; private set; }

        public string SelectedArtist
        {
            get { return this.selectedArtist; }
            set { this.RaiseAndSetIfChanged(ref this.selectedArtist, value); }
        }

        public async Task<IReadOnlyList<Song>> LoadSongsAsync()
        {
            IReadOnlyList<Song> songs = await NetworkMessenger.Instance.GetSongsAsync();

            await BlobCache.InMemory.InsertObject("songs", songs);

            return songs;
        }
    }
}