using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Espera.Android
{
    public class MainViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<IReadOnlyList<string>> artists;
        private readonly ObservableAsPropertyHelper<IReadOnlyList<Song>> songs;
        private IPAddress ipAdress;

        private string selectedArtist;

        public MainViewModel()
        {
            this.LoadArtistsCommand = new ReactiveCommand();
            this.songs = this.LoadArtistsCommand.RegisterAsyncTask(_ => this.LoadSongsAsync())
                .ToProperty(this, x => x.Songs);

            this.artists = this.songs
               .Select(x => x.GroupBy(s => s.Artist).Select(g => g.Key).Distinct(StringComparer.InvariantCultureIgnoreCase).ToList())
               .ToProperty(this, x => x.Artists, new List<string>());
        }

        public IReadOnlyList<string> Artists
        {
            get { return this.artists.Value; }
        }

        public IPAddress IpAddress
        {
            get { return this.ipAdress; }
            set { this.RaiseAndSetIfChanged(ref this.ipAdress, value); }
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

        private async Task<IReadOnlyList<Song>> LoadSongsAsync()
        {
            if (this.IpAddress == null)
            {
                this.ipAdress = await NetworkMessenger.DiscoverServer();
            }

            using (var messenger = new NetworkMessenger(this.IpAddress))
            {
                await messenger.ConnectAsync();
                return await messenger.GetSongsAsync();
            }
        }
    }
}