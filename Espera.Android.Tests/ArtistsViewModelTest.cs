using Espera.Mobile.Core;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.ViewModels;
using Microsoft.Reactive.Testing;
using Moq;
using ReactiveUI;
using ReactiveUI.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Espera.Android.Tests
{
    public class ArtistsViewModelTest
    {
        [Fact]
        public void LoadCommandSmokeTest()
        {
            var songs = SetupSongsWithArtist("B", "b", "C", "A").ToReadOnlyList();

            var messenger = new Mock<INetworkMessenger>();
            messenger.Setup(x => x.GetSongsAsync()).Returns(songs.ToTaskResult());

            NetworkMessenger.Override(messenger.Object);

            var vm = new ArtistsViewModel();

            vm.LoadCommand.Execute(null);

            Assert.True(new[] { "A", "B", "C" }.SequenceEqual(vm.Artists));
        }

        [Fact]
        public void LoadCommandTimeoutTriggersMessages()
        {
            var messenger = new Mock<INetworkMessenger>();
            messenger.Setup(x => x.GetSongsAsync()).Returns(async () =>
                {
                    await Task.Delay(1000);
                    return null;
                });

            NetworkMessenger.Override(messenger.Object);

            var vm = new ArtistsViewModel();

            var coll = vm.Messages.CreateCollection();

            (new TestScheduler()).With(scheduler =>
            {
                vm.LoadCommand.Execute(null);
                scheduler.AdvanceByMs(15000);
            });

            Assert.Equal(1, coll.Count);
        }

        private static IEnumerable<Song> SetupSongsWithArtist(params string[] artists)
        {
            return artists.Select(SetupSongWithArtist);
        }

        private static Song SetupSongWithArtist(string artist)
        {
            return new Song(artist, String.Empty, String.Empty, String.Empty, TimeSpan.Zero, Guid.NewGuid(), SongSource.Local);
        }
    }
}