using Akavache;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;

namespace Espera.Android
{
    public class MainViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<IReadOnlyList<string>> artists;
        private readonly ObservableAsPropertyHelper<IReadOnlyList<Song>> songs;

        private string selectedArtist;

        public MainViewModel()
        {
            this.LoadArtistsCommand = new ReactiveCommand();
            this.songs = this.LoadArtistsCommand.Select(x => this.LoadSongsAsync())
                .Merge()
                .ToProperty(this, x => x.Songs);

            this.artists = this.songs
               .Select(x => x.GroupBy(s => s.Artist).Select(g => g.Key).Distinct(StringComparer.InvariantCultureIgnoreCase).ToList())
               .ToProperty(this, x => x.Artists, new List<string>());
        }

        public IReadOnlyList<string> Artists
        {
            get { return this.artists.Value; }
        }

        public ReactiveCommand LoadArtistsCommand { get; private set; }

        public string SelectedArtist
        {
            get { return this.selectedArtist; }
            set { this.RaiseAndSetIfChanged(ref this.selectedArtist, value); }
        }

        public IReadOnlyList<Song> Songs
        {
            get { return this.songs.Value; }
        }

        private IObservable<IReadOnlyList<Song>> LoadSongsAsync()
        {
            return Observable.StartAsync(async () =>
            {
                if (!NetworkMessenger.Instance.Connected)
                {
                    IPAddress address = await NetworkMessenger.DiscoverServer();

                    await NetworkMessenger.Instance.ConnectAsync(address);
                }
            })
            .Select(x => BlobCache.InMemory.GetAndFetchLatest("songs", () => NetworkMessenger.Instance.GetSongsAsync()))
            .Merge();
        }
    }
}