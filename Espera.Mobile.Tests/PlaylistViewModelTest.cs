using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
using Microsoft.Reactive.Testing;
using NSubstitute;
using ReactiveUI;
using ReactiveUI.Testing;
using Xunit;

namespace Espera.Android.Tests
{
    public class PlaylistViewModelTest
    {
        [Fact]
        public async Task PlaylistChangeUpdatesPlaylist()
        {
            var playlist = new NetworkPlaylist
            {
                Name = "A",
                Songs = new List<NetworkSong>().AsReadOnly()
            };

            var messenger = CreateDefaultPlaylistMessenger();
            messenger.GetCurrentPlaylistAsync().Returns(playlist.ToTaskResult());

            var songs = Helpers.SetupSongs(2);
            var changedPlaylist = new NetworkPlaylist
            {
                Name = "B",
                Songs = songs,
                CurrentIndex = 1
            };

            var playlistChanged = new Subject<NetworkPlaylist>();
            messenger.PlaylistChanged.Returns(playlistChanged);

            var vm = new PlaylistViewModel();
            vm.Activator.Activate();

            await vm.LoadPlaylistCommand.ExecuteAsync();

            playlistChanged.OnNext(changedPlaylist);

            Assert.Equal(1, changedPlaylist.CurrentIndex);
            //Assert.Equal(playlist.Name, vm.Name);
            Assert.Equal(changedPlaylist.Songs.Count, vm.Entries.Count);
        }

        private static INetworkMessenger CreateDefaultPlaylistMessenger()
        {
            var messenger = Substitute.For<INetworkMessenger>();
            messenger.PlaybackStateChanged.Returns(Observable.Never<NetworkPlaybackState>());
            messenger.PlaylistChanged.Returns(Observable.Never<NetworkPlaylist>());
            messenger.AccessPermission.Returns(Observable.Return(NetworkAccessPermission.Admin));

            messenger.RemainingVotesChanged.Returns(Observable.Never<int?>());

            NetworkMessenger.Override(messenger);

            return messenger;
        }

        public class TheLoadPlaylistCommand
        {
            [Fact]
            public void HasTimeout()
            {
                var messenger = CreateDefaultPlaylistMessenger();
                messenger.GetCurrentPlaylistAsync().Returns(Observable.Never<NetworkPlaylist>().ToTask());

                var vm = new PlaylistViewModel();
                vm.Activator.Activate();

                var thrown = vm.LoadPlaylistCommand.ThrownExceptions.CreateCollection();

                (new TestScheduler()).With(scheduler =>
                {
                    vm.LoadPlaylistCommand.Execute(null);
                    scheduler.AdvanceByMs(15000);
                });

                Assert.Equal(1, thrown.Count);
            }

            [Fact]
            public void SmokeTest()
            {
                var songs = Helpers.SetupSongs(2);
                var playlist = new NetworkPlaylist
                {
                    Name = "A",
                    Songs = songs,
                    CurrentIndex = 1
                };

                var messenger = CreateDefaultPlaylistMessenger();
                messenger.GetCurrentPlaylistAsync().Returns(playlist.ToTaskResult());

                var vm = new PlaylistViewModel();
                vm.Activator.Activate();

                vm.LoadPlaylistCommand.Execute(null);

                Assert.Equal(playlist.Songs.Count, vm.Entries.Count);
            }
        }

        public class ThePlayNextSongCommand
        {
            [Fact]
            public void CanExecuteIsFalseForEmptyPlaylist()
            {
                var playlist = new NetworkPlaylist
                {
                    Name = "A",
                    Songs = new List<NetworkSong>().AsReadOnly(),
                    CurrentIndex = 0
                };

                var messenger = CreateDefaultPlaylistMessenger();
                messenger.GetCurrentPlaylistAsync().Returns(playlist.ToTaskResult());
                messenger.PlayNextSongAsync().Returns(new ResponseInfo { Status = ResponseStatus.Success }.ToTaskResult());

                var vm = new PlaylistViewModel();
                vm.Activator.Activate();

                vm.LoadPlaylistCommand.Execute(null);

                Assert.False(vm.PlayNextSongCommand.CanExecute(null));
            }

            [Fact]
            public void SmokeTest()
            {
                var songs = Helpers.SetupSongs(2);
                var playlist = new NetworkPlaylist
                {
                    Name = "A",
                    Songs = songs,
                    CurrentIndex = 0
                };

                var messenger = CreateDefaultPlaylistMessenger();
                messenger.GetCurrentPlaylistAsync().Returns(playlist.ToTaskResult());
                messenger.PlayNextSongAsync().Returns(new ResponseInfo { Status = ResponseStatus.Success }.ToTaskResult());

                var playlists = new Subject<NetworkPlaylist>();
                messenger.PlaylistChanged.Returns(playlists);

                var vm = new PlaylistViewModel();
                vm.Activator.Activate();

                vm.LoadPlaylistCommand.Execute(null);

                var canExecute = vm.PlayNextSongCommand.CanExecuteObservable.CreateCollection();

                vm.PlayNextSongCommand.Execute(null);

                playlists.OnNext(new NetworkPlaylist
                {
                    Name = "A",
                    Songs = songs,
                    CurrentIndex = 1
                });

                playlists.OnNext(new NetworkPlaylist
                {
                    Name = "A",
                    Songs = songs,
                    CurrentIndex = 0
                });

                playlists.OnNext(new NetworkPlaylist
                {
                    Name = "A",
                    Songs = songs,
                });

                messenger.Received(1).PlayNextSongAsync();
                Assert.Equal(new[] { false, true, false, true, false, true, false }, canExecute);
            }
        }

        public class ThePlayPauseCommand
        {
            [Fact]
            public void SmokeTest()
            {
                var songs = Helpers.SetupSongs(2);
                var playlist = new NetworkPlaylist
                {
                    Name = "A",
                    Songs = songs,
                    CurrentIndex = 0
                };

                var messenger = CreateDefaultPlaylistMessenger();
                messenger.GetCurrentPlaylistAsync().Returns(playlist.ToTaskResult());
                messenger.ContinueSongAsync().Returns(new ResponseInfo { Status = ResponseStatus.Success }.ToTaskResult());
                messenger.PauseSongAsync().Returns(new ResponseInfo { Status = ResponseStatus.Success }.ToTaskResult());

                var playbackState = new Subject<NetworkPlaybackState>();
                messenger.PlaybackStateChanged.Returns(playbackState);

                var vm = new PlaylistViewModel();
                vm.Activator.Activate();

                vm.LoadPlaylistCommand.Execute(null);

                var canExecute = vm.PlayPauseCommand.CanExecuteObservable.CreateCollection();

                playbackState.OnNext(NetworkPlaybackState.Paused);

                vm.PlayPauseCommand.Execute(null);
                messenger.Received(1).ContinueSongAsync();

                playbackState.OnNext(NetworkPlaybackState.Playing);

                vm.PlayPauseCommand.Execute(null);
                messenger.Received(1).PauseSongAsync();

                playbackState.OnNext(NetworkPlaybackState.Paused);

                Assert.Equal(new[] { false, true, false, true, false, true }, canExecute);
            }
        }

        public class ThePlayPlaylistSongCommand
        {
            [Fact]
            public async Task SmokeTest()
            {
                var songs = Helpers.SetupSongs(2);
                var playlist = new NetworkPlaylist
                {
                    Name = "A",
                    Songs = songs,
                    CurrentIndex = 0
                };

                var messenger = CreateDefaultPlaylistMessenger();
                messenger.GetCurrentPlaylistAsync().Returns(playlist.ToTaskResult());
                messenger.PlayPlaylistSongAsync(songs[1].Guid)
                    .Returns(new ResponseInfo { Status = ResponseStatus.Success }.ToTaskResult());

                var vm = new PlaylistViewModel();
                vm.Activator.Activate();
                var coll = vm.Message.CreateCollection();

                await vm.LoadPlaylistCommand.ExecuteAsync();

                vm.SelectedEntry = vm.Entries[1];

                await vm.PlayPlaylistSongCommand.ExecuteAsync();

                messenger.Received(1).PlayPlaylistSongAsync(songs[1].Guid);
                Assert.Equal(1, coll.Count);
            }
        }

        public class ThePlayPreviousSongCommand
        {
            [Fact]
            public void CanExecuteIsFalseForEmptyPlaylist()
            {
                var playlist = new NetworkPlaylist
                {
                    Name = "A",
                    Songs = new List<NetworkSong>().AsReadOnly(),
                    CurrentIndex = 0
                };

                var messenger = CreateDefaultPlaylistMessenger();
                messenger.GetCurrentPlaylistAsync().Returns(playlist.ToTaskResult());
                messenger.PlayNextSongAsync().Returns(new ResponseInfo { Status = ResponseStatus.Success }.ToTaskResult());

                var vm = new PlaylistViewModel();
                vm.Activator.Activate();

                vm.LoadPlaylistCommand.Execute(null);

                Assert.False(vm.PlayPreviousSongCommand.CanExecute(null));
            }

            [Fact]
            public void SmokeTest()
            {
                var songs = Helpers.SetupSongs(2);
                var playlist = new NetworkPlaylist
                {
                    Name = "A",
                    Songs = songs,
                    CurrentIndex = 1
                };

                var messenger = CreateDefaultPlaylistMessenger();
                messenger.GetCurrentPlaylistAsync().Returns(playlist.ToTaskResult());
                messenger.PlayPreviousSongAsync().Returns(new ResponseInfo { Status = ResponseStatus.Success }.ToTaskResult());

                var playlists = new Subject<NetworkPlaylist>();
                messenger.PlaylistChanged.Returns(playlists);

                var vm = new PlaylistViewModel();
                vm.Activator.Activate();

                vm.LoadPlaylistCommand.Execute(null);

                var canExecute = vm.PlayPreviousSongCommand.CanExecuteObservable.CreateCollection();

                vm.PlayPreviousSongCommand.Execute(null);

                playlists.OnNext(new NetworkPlaylist
                {
                    Name = "A",
                    Songs = songs,
                    CurrentIndex = 0
                });
                playlists.OnNext(new NetworkPlaylist
                {
                    Name = "A",
                    Songs = songs,
                    CurrentIndex = 1
                });
                playlists.OnNext(new NetworkPlaylist
                {
                    Name = "A",
                    Songs = songs,
                });

                messenger.Received(1).PlayPreviousSongAsync();
                Assert.Equal(new[] { false, true, false, true, false, true, false }, canExecute);
            }
        }
    }
}