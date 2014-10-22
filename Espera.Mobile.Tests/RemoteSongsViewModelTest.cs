using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
using NSubstitute;
using Xunit;

namespace Espera.Android.Tests
{
    public class RemoteSongsViewModelTest
    {
        public class TheAddToPlaylistCommand
        {
            [Fact]
            public async Task SmokeTest()
            {
                var messenger = Substitute.For<INetworkMessenger>();
                messenger.AddSongToPlaylistAsync(Arg.Any<Guid>()).Returns(new ResponseInfo().ToTaskResult());
                NetworkMessenger.Override(messenger);
                var songs = Helpers.SetupSongs(4).ToList();

                var vm = new RemoteSongsViewModel(songs);
                vm.Activator.Activate();
                vm.SelectedSong = vm.Songs[2];

                await vm.AddToPlaylistCommand.ExecuteAsync();

                messenger.Received().AddSongToPlaylistAsync(songs[2].Guid);
            }
        }

        public class ThePlaySongsCommand
        {
            [Fact]
            public async Task SmokeTest()
            {
                var messenger = Substitute.For<INetworkMessenger>();
                NetworkMessenger.Override(messenger);
                var songs = Helpers.SetupSongs(4).ToList();

                var vm = new RemoteSongsViewModel(songs);
                vm.Activator.Activate();
                vm.SelectedSong = vm.Songs[2];

                await vm.PlaySongsCommand.ExecuteAsync();

                var guids = new[] { songs[2].Guid, songs[3].Guid };

                messenger.Received().PlaySongsAsync(Arg.Is<IEnumerable<Guid>>(x => x.SequenceEqual(guids)));
            }
        }
    }
}