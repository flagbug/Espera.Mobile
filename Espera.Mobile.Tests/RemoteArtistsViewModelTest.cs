using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using Espera.Mobile.Core;
using Espera.Mobile.Core.SongFetchers;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
using NSubstitute;
using ReactiveUI;
using Xunit;

namespace Espera.Android.Tests
{
    public class RemoteArtistsViewModelTest
    {
        public class TheLoadCommand
        {
            [Fact]
            public async Task CachesAllSongs()
            {
                var songs = Helpers.SetupSongs(3);

                var songFetcher = Substitute.For<ISongFetcher<NetworkSong>>();
                songFetcher.GetSongsAsync().Returns(Observable.Return(songs));

                var vm = new RemoteArtistsViewModel(songFetcher);
                vm.Activator.Activate();

                await vm.LoadCommand.ExecuteAsync();

                var cachedSongs = await BlobCache.LocalMachine.GetObject<List<NetworkSong>>(BlobCacheKeys.RemoteSongs);

                Assert.Equal(3, cachedSongs.Count);
            }

            [Fact]
            public async Task DistinctsFetchedSongsByGuid()
            {
                var songs = Helpers.SetupSongs(2);
                songs[0].Artist = "A";
                songs[1].Artist = "B";

                var songFetcher = Substitute.For<ISongFetcher<NetworkSong>>();
                songFetcher.GetSongsAsync().Returns(Observable.Return(songs).Concat(Observable.Return(songs)));

                var vm = new RemoteArtistsViewModel(songFetcher);
                vm.Activator.Activate();

                var loadResults = vm.LoadCommand.CreateCollection();

                await vm.LoadCommand.ExecuteAsync();

                Assert.Equal(1, loadResults.Count);
            }

            [Fact]
            public async Task FetchesSongsAfterCachedReturned()
            {
                var songs1 = Helpers.SetupSongs(1);
                songs1[0].Artist = "A";

                var songs2 = Helpers.SetupSongs(2);
                songs2[0].Artist = "B";
                songs2[1].Artist = "C";

                var songFetcher = Substitute.For<ISongFetcher<NetworkSong>>();
                songFetcher.GetSongsAsync().Returns(Observable.Return(songs1).Concat(Observable.Return(songs2)));

                var vm = new RemoteArtistsViewModel(songFetcher);
                vm.Activator.Activate();

                var artists = vm.WhenAnyValue(x => x.Artists).CreateCollection();

                await vm.LoadCommand.ExecuteAsync();

                Assert.Equal(1, artists[1].Count);
                Assert.Equal(2, artists[2].Count);
            }
        }
    }
}