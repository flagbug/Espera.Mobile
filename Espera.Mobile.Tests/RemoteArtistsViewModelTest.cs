using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using Espera.Mobile.Core;
using Espera.Mobile.Core.SongFetchers;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
using NSubstitute;
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

                await vm.LoadCommand.ExecuteAsync();

                var cachedSongs = await BlobCache.LocalMachine.GetObject<List<NetworkSong>>(BlobCacheKeys.RemoteSongs);

                Assert.Equal(3, cachedSongs.Count);
            }
        }
    }
}