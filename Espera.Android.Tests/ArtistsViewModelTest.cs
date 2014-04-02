using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
using Microsoft.Reactive.Testing;
using Moq;
using ReactiveUI;
using ReactiveUI.Testing;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Espera.Android.Tests
{
    public class ArtistsViewModelTest
    {
        [Fact]
        public async Task LoadCommandSmokeTest()
        {
            var songs = SetupSongsWithArtist("B", "b", "C", "A").ToReadOnlyList();

            var messenger = new Mock<INetworkMessenger>();
            messenger.Setup(x => x.GetSongsAsync()).Returns(songs.ToTaskResult());

            NetworkMessenger.Override(messenger.Object);

            var vm = new ArtistsViewModel();

            await vm.LoadCommand.ExecuteAsync();

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