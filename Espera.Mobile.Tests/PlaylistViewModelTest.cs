using System;
using System.Collections.Generic;
using System.ComponentModel;
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

            messenger.GetGuestSystemInfo().Returns(Task.FromResult(new GuestSystemInfo { IsEnabled = false }));

            messenger.PlaybackStateChanged.Returns(Observable.Never<NetworkPlaybackState>());
            messenger.PlaylistChanged.Returns(Observable.Never<NetworkPlaylist>());
            messenger.AccessPermission.Returns(NetworkAccessPermission.Admin);
            messenger.GuestSystemInfoChanged.Returns(Observable.Never<GuestSystemInfo>());

            NetworkMessenger.Override(messenger);

            return messenger;
        }

        public class TheCanModifyProperty
        {
            [Fact]
            public void IsDeterminedByNetworkAccessPermission()
            {
                var messenger = CreateDefaultPlaylistMessenger();
                messenger.AccessPermission.Returns(NetworkAccessPermission.Admin);

                var vm = new PlaylistViewModel();
                vm.Activator.Activate();

                Assert.True(vm.CanModify);

                messenger.AccessPermission.Returns(NetworkAccessPermission.Guest);
                messenger.PropertyChanged += Raise.Event<PropertyChangedEventHandler>(new PropertyChangedEventArgs("AccessPermission"));

                Assert.False(vm.CanModify);
            }
        }

        public class TheCurrentSongProperty
        {
            [Fact]
            public async Task IsCurrentPlaylingSong()
            {
                var song = new NetworkSong { Title = "A" };
                var playlist = new NetworkPlaylist
                {
                    Name = "A",
                    Songs = new List<NetworkSong> { song }.AsReadOnly(),
                    CurrentIndex = 0
                };

                var messenger = CreateDefaultPlaylistMessenger();
                messenger.GetCurrentPlaylistAsync().Returns(playlist.ToTaskResult());

                var vm = new PlaylistViewModel();
                vm.Activator.Activate();

                await vm.LoadPlaylistCommand.ExecuteAsync();

                Assert.Equal("A", vm.CurrentSong.Title);
            }
        }

        public class TheCurrentTimeSecondsProperty
        {
            [Fact]
            public void DoesNotDistinctTimeOutOfWindow()
            {
                var messenger = CreateDefaultPlaylistMessenger();
                NetworkMessenger.Override(messenger);

                new TestScheduler().With(sched =>
                {
                    var vm = new PlaylistViewModel();
                    vm.Activator.Activate();

                    vm.CurrentTimeSeconds = 60;

                    sched.AdvanceByMs(PlaylistViewModel.TimeThrottleDuration.TotalMilliseconds + 1);

                    vm.CurrentTimeSeconds = 60;

                    sched.AdvanceByMs(PlaylistViewModel.TimeThrottleDuration.TotalMilliseconds + 1);

                    messenger.Received(2).SetCurrentTime(TimeSpan.FromSeconds(60));
                });
            }

            [Fact]
            public void SendsFirstTimeImmediatelyToNetwork()
            {
                var messenger = CreateDefaultPlaylistMessenger();
                NetworkMessenger.Override(messenger);

                new TestScheduler().With(sched =>
                {
                    var vm = new PlaylistViewModel();
                    vm.Activator.Activate();

                    vm.CurrentTimeSeconds = 60;

                    sched.AdvanceByMs(1);

                    messenger.Received().SetCurrentTime(TimeSpan.FromSeconds(60));
                });
            }

            [Fact]
            public void SendsTimeToNetworkOnlyIfChangedInWindow()
            {
                var messenger = CreateDefaultPlaylistMessenger();
                NetworkMessenger.Override(messenger);

                new TestScheduler().With(sched =>
                {
                    var vm = new PlaylistViewModel();
                    vm.Activator.Activate();

                    vm.CurrentTimeSeconds = 60;
                    vm.CurrentTimeSeconds = 60;

                    sched.AdvanceByMs(PlaylistViewModel.TimeThrottleDuration.TotalMilliseconds + 1);

                    messenger.Received(1).SetCurrentTime(TimeSpan.FromSeconds(60));
                });
            }

            [Fact]
            public void ThrottlesTimeChangesToNetworkByCount()
            {
                var messenger = CreateDefaultPlaylistMessenger();
                NetworkMessenger.Override(messenger);

                var vm = new PlaylistViewModel();
                vm.Activator.Activate();

                for (int i = 1; i < PlaylistViewModel.TimeThrottleCount + 1; i++)
                {
                    vm.CurrentTimeSeconds = i;
                }

                messenger.ReceivedWithAnyArgs(2).SetCurrentTime(Arg.Any<TimeSpan>());
                messenger.Received(1).SetCurrentTime(TimeSpan.FromSeconds(1));
                messenger.Received(1).SetCurrentTime(TimeSpan.FromSeconds(PlaylistViewModel.TimeThrottleCount));
            }

            [Fact]
            public void ThrottlesTimeChangesToNetworkByTime()
            {
                var messenger = CreateDefaultPlaylistMessenger();
                NetworkMessenger.Override(messenger);

                new TestScheduler().With(sched =>
                {
                    var vm = new PlaylistViewModel();
                    vm.Activator.Activate();

                    vm.CurrentTimeSeconds = 10;

                    sched.AdvanceByMs(1);

                    vm.CurrentTimeSeconds = 20;

                    sched.AdvanceByMs(1);

                    vm.CurrentTimeSeconds = 30;

                    sched.AdvanceByMs(PlaylistViewModel.TimeThrottleDuration.TotalMilliseconds + 1);

                    messenger.ReceivedWithAnyArgs(2).SetCurrentTime(Arg.Any<TimeSpan>());
                    messenger.Received(1).SetCurrentTime(TimeSpan.FromSeconds(10));
                    messenger.Received(1).SetCurrentTime(TimeSpan.FromSeconds(30));
                });
            }
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
            public async Task LoadsGuestSystemInfo()
            {
                var playlist = new NetworkPlaylist
                {
                    Name = "A",
                    Songs = new List<NetworkSong>().AsReadOnly(),
                    CurrentIndex = 0
                };

                var messenger = CreateDefaultPlaylistMessenger();
                messenger.GetCurrentPlaylistAsync().Returns(playlist.ToTaskResult());
                messenger.GetGuestSystemInfo().Returns(Task.FromResult(new GuestSystemInfo { IsEnabled = true, RemainingVotes = 5 }));

                var vm = new PlaylistViewModel();
                vm.Activator.Activate();

                Assert.Null(vm.RemainingVotes);

                await vm.LoadPlaylistCommand.ExecuteAsync();

                Assert.Equal(5, vm.RemainingVotes);
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
                Assert.Equal(new[] { true, false, true, false, true, false }, canExecute);
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
                Assert.Equal(new[] { true, false, true, false, true, false }, canExecute);
            }
        }
    }
}