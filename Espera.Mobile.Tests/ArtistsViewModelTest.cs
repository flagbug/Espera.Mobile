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
                vm.Activator.Activate();

                await vm.LoadCommand.ExecuteAsync();

                Assert.Equal(new[] { "A", "B", "C" }.AsEnumerable(), vm.Artists.AsEnumerable());
            }

            [Fact]
            public void TimeoutTriggersthrownExceptions()
            {
                var songFetcher = Substitute.For<ISongFetcher<NetworkSong>>();
                songFetcher.GetSongsAsync().Returns(Observable.Never<IReadOnlyList<NetworkSong>>());

                var vm = new ArtistsViewModel<NetworkSong>(songFetcher, "AnyKey");
                vm.Activator.Activate();

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

        public class TheSearchTermProperty
        {
            [Fact]
            public async Task FiltersArtists()
            {
                var songs = SetupSongsWithArtist("A", "B").ToReadOnlyList();

                var songFetcher = Substitute.For<ISongFetcher<NetworkSong>>();
                songFetcher.GetSongsAsync().Returns(Observable.Return(songs));

                var vm = new ArtistsViewModel<NetworkSong>(songFetcher, "key");
                vm.Activator.Activate();

                await vm.LoadCommand.ExecuteAsync();

                vm.SearchTerm = "A";
                Assert.Equal("A", vm.Artists.Single());

                vm.SearchTerm = "B";
                Assert.Equal("B", vm.Artists.Single());

                vm.SearchTerm = "C";
                Assert.Equal(0, vm.Artists.Count);
            }

            [Fact]
            public async Task NullOrWhiteSpaceDoesntFilter()
            {
                var songs = SetupSongsWithArtist("A", "B").ToReadOnlyList();

                var songFetcher = Substitute.For<ISongFetcher<NetworkSong>>();
                songFetcher.GetSongsAsync().Returns(Observable.Return(songs));

                var vm = new ArtistsViewModel<NetworkSong>(songFetcher, "key");
                vm.Activator.Activate();

                await vm.LoadCommand.ExecuteAsync();

                vm.SearchTerm = null;
                Assert.Equal(2, vm.Artists.Count);

                vm.SearchTerm = String.Empty;
                Assert.Equal(2, vm.Artists.Count);

                vm.SearchTerm = "   ";
                Assert.Equal(2, vm.Artists.Count);
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
                vm.Activator.Activate();

                await vm.LoadCommand.ExecuteAsync();

                vm.SelectedArtist = "A";

                List<NetworkSong> cached = await BlobCache.LocalMachine.GetObject<List<NetworkSong>>("TheKey");

                Assert.Equal(2, cached.Count);
                Assert.True(cached.All(x => x.Artist == "A"));
            }

            [Fact]
            public async Task FiltersWithSearchTerm()
            {
                IReadOnlyList<NetworkSong> songs = SetupSongsWithArtist("A", "A", "B").ToList();
                songs[0].Title = "C";
                songs[1].Title = "D";

                var songFetcher = Substitute.For<ISongFetcher<NetworkSong>>();
                songFetcher.GetSongsAsync().Returns(Observable.Return(songs));

                var vm = new ArtistsViewModel<NetworkSong>(songFetcher, "TheKey");
                vm.Activator.Activate();

                await vm.LoadCommand.ExecuteAsync();

                vm.SearchTerm = "C";
                vm.SelectedArtist = "A";

                List<NetworkSong> cached = await BlobCache.LocalMachine.GetObject<List<NetworkSong>>("TheKey");

                Assert.Equal(1, cached.Count);
                Assert.Equal("C", songs[0].Title);
            }
        }
    }
}