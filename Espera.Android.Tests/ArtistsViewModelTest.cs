using Espera.Mobile.Core.SongFetchers;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
using Microsoft.Reactive.Testing;
using Moq;
using ReactiveUI;
using ReactiveUI.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Xunit;

namespace Espera.Android.Tests
{
    public class ArtistsViewModelTest
    {
        [Fact]
        public void LoadCommandSmokeTest()
        {
            var songs = SetupSongsWithArtist("B", "b", "C", "A").ToReadOnlyList();

            var songFetcher = new Mock<ISongFetcher>();
            songFetcher.Setup(x => x.GetSongsAsync()).Returns(Observable.Return(songs));

            var vm = new ArtistsViewModel(songFetcher.Object);

            vm.LoadCommand.Execute(null);

            Assert.True(new[] { "A", "B", "C" }.SequenceEqual(vm.Artists));
        }

        [Fact]
        public void LoadCommandTimeoutTriggersMessages()
        {
            var songFetcher = new Mock<ISongFetcher>();
            songFetcher.Setup(x => x.GetSongsAsync()).Returns(Observable.Never<IReadOnlyList<NetworkSong>>()
                .Timeout(TimeSpan.FromSeconds(10)));

            var vm = new ArtistsViewModel(songFetcher.Object);

            var coll = vm.Messages.CreateCollection();

            (new TestScheduler()).With(scheduler =>
            {
                vm.LoadCommand.Execute(null);
                scheduler.AdvanceByMs(15000);
            });

            Assert.Equal(1, coll.Count);
        }

        private static IEnumerable<NetworkSong> SetupSongsWithArtist(params string[] artists)
        {
            return artists.Select(SetupSongWithArtist);
        }

        private static NetworkSong SetupSongWithArtist(string artist)
        {
            NetworkSong song = Helpers.SetupSong();
            song.Artist = artist;

            return song;
        }
    }
}