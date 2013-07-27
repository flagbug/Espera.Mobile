using ReactiveUI;
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
        private readonly ObservableAsPropertyHelper<string> ipAdress;
        private readonly ObservableAsPropertyHelper<IReadOnlyList<Song>> songs;

        public MainViewModel()
        {
            this.DiscoverServerCommand = new ReactiveCommand();
            this.ipAdress = this.DiscoverServerCommand.RegisterAsyncTask(_ => NetworkMessenger.DiscoverServer())
                .Select(x => x.ToString())
                .ToProperty(this, x => x.IpAddress);

            this.LoadArtistsCommand = new ReactiveCommand(this.ipAdress.Select(x => x != null).StartWith(false));
            this.songs = this.LoadArtistsCommand.RegisterAsyncTask(_ => this.LoadSongsAsync())
                .ToProperty(this, x => x.Songs);

            this.artists = this.songs
               .Select(x => x.GroupBy(s => s.Artist).Select(g => g.Key).Distinct().ToList())
               .ToProperty(this, x => x.Artists, new List<string>());
        }

        public IReadOnlyList<string> Artists
        {
            get { return this.artists.Value; }
        }

        public IReactiveCommand DiscoverServerCommand { get; private set; }

        public string IpAddress
        {
            get { return this.ipAdress.Value; }
        }

        public ReactiveCommand LoadArtistsCommand { get; private set; }

        public IReadOnlyList<Song> Songs
        {
            get { return this.songs.Value; }
        }

        private async Task<IReadOnlyList<Song>> LoadSongsAsync()
        {
            using (var messenger = new NetworkMessenger(IPAddress.Parse(this.IpAddress)))
            {
                await messenger.ConnectAsync();
                return await messenger.GetSongsAsync();
            }
        }
    }
}