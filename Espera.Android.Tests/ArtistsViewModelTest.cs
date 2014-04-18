using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Espera.Mobile.Core.SongFetchers;
using Espera.Mobile.Core.Songs;
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
        private static IEnumerable<Song> SetupSongsWithArtist(params string[] artists)
        {
            return artists.Select(SetupSongWithArtist);
        }

        private static Song SetupSongWithArtist(string artist)
        {
            NetworkSong song = Helpers.SetupSong();
            song.Artist = artist;

            return new LocalSong(song.Title, song.Artist, song.Album, "0");
        }

        public class TheLoadCommand
        {
            [Fact]
            public async Task SmokeTest()
            {
                var songs = SetupSongsWithArtist("B", "b", "C", "A").ToReadOnlyList();

                var songFetcher = Substitute.For<ISongFetcher<Song>>();
                songFetcher.GetSongsAsync().Returns(Observable.Return(songs));

                var vm = new ArtistsViewModel<Song>(songFetcher);

                await vm.LoadCommand.ExecuteAsync();

                Assert.True(new[] { "A", "B", "C" }.SequenceEqual(vm.Artists));
            }

            [Fact]
            public void TimeoutTriggersMessages()
            {
                var songFetcher = Substitute.For<ISongFetcher<Song>>();
                songFetcher.GetSongsAsync().Returns(Observable.Never<IReadOnlyList<Song>>()
                    .Timeout(TimeSpan.FromSeconds(10)));

                var vm = new ArtistsViewModel<Song>(songFetcher);

                var coll = vm.Messages.CreateCollection();

                (new TestScheduler()).With(scheduler =>
                {
                    vm.LoadCommand.Execute(null);
                    scheduler.AdvanceByMs(15000);
                });

                Assert.Equal(1, coll.Count);
            }
        }
    }
}