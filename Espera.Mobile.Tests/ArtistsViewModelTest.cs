using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using Espera.Mobile.Core;
using Espera.Mobile.Core.SongFetchers;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
using Microsoft.Reactive.Testing;
using NSubstitute;
using ReactiveUI;
using ReactiveUI.Testing;
using Xunit;

namespace Espera.Android.Tests
{
    public class ArtistsViewModelTest
    {
        private static IEnumerable<NetworkSong> SetupSongsWithArtist(params string[] artists)
        {
            return artists.Select(SetupSongWithArtist);
        }

        private static NetworkSong SetupSongWithArtist(string artist)
        {
            NetworkSong song = Helpers.SetupSong();
            song.Artist = artist;

            return new LocalSong(song.Title, song.Artist, song.Album, song.Genre, song.Duration, 0, "0");
        }

        public class TheLoadCommand
        {
            [Fact]
            public async Task SmokeTest()
            {
                var songs = SetupSongsWithArtist("B", "b", "C", "A").ToReadOnlyList();

                var songFetcher = Substitute.For<ISongFetcher<NetworkSong>>();
                songFetcher.GetSongsAsync().Returns(Observable.Return(songs));

                var vm = new ArtistsViewModel<NetworkSong>(songFetcher, "AnyKey");

                await vm.LoadCommand.ExecuteAsync();

                Assert.Equal(new[] { "A", "B", "C" }.AsEnumerable(), vm.Artists.AsEnumerable());
            }

            [Fact]
            public void TimeoutTriggersthrownExceptions()
            {
                var songFetcher = Substitute.For<ISongFetcher<NetworkSong>>();
                songFetcher.GetSongsAsync().Returns(Observable.Never<IReadOnlyList<NetworkSong>>());

                var vm = new ArtistsViewModel<NetworkSong>(songFetcher, "AnyKey");

                var coll = vm.LoadCommand.ThrownExceptions.CreateCollection();

                (new TestScheduler()).With(scheduler =>
                {
                    vm.LoadCommand.Execute(null);
                    scheduler.AdvanceByMs(ArtistsViewModel<NetworkSong>.LoadCommandTimeout.TotalMilliseconds + 1);
                });

                Assert.Equal(1, coll.Count);
                Assert.IsType<TimeoutException>(coll[0]);
            }
        }

        public class TheSelectedArtistProperty
        {
            [Fact]
            public async Task FiltersSongsAndStoresThemInLocalCache()
            {
                IReadOnlyList<NetworkSong> songs = SetupSongsWithArtist("A", "A", "B").ToList();

                var songFetcher = Substitute.For<ISongFetcher<NetworkSong>>();
                songFetcher.GetSongsAsync().Returns(Observable.Return(songs));

                var vm = new ArtistsViewModel<NetworkSong>(songFetcher, "TheKey");

                await vm.LoadCommand.ExecuteAsync();

                vm.SelectedArtist = "A";

                List<NetworkSong> cached = await BlobCache.LocalMachine.GetObject<List<NetworkSong>>("TheKey");

                Assert.Equal(2, cached.Count);
                Assert.True(cached.All(x => x.Artist == "A"));
            }
        }
    }
}