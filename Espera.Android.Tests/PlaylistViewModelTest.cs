using Espera.Android.Network;
using Espera.Android.ViewModels;
using Microsoft.Reactive.Testing;
using Moq;
using ReactiveUI;
using ReactiveUI.Testing;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Xunit;

namespace Espera.Android.Tests
{
    public class PlaylistViewModelTest
    {
        [Fact]
        public void LoadPlaylistCommandHasTimeout()
        {
            var messenger = CreateDefaultPlaylistMessenger();
            messenger.Setup(x => x.GetCurrentPlaylistAsync()).Returns(async () =>
            {
                await Task.Delay(1000);
                return null;
            });

            var vm = new PlaylistViewModel();

            var thrown = vm.LoadPlaylistCommand.ThrownExceptions.CreateCollection();

            (new TestScheduler()).With(scheduler =>
            {
                vm.LoadPlaylistCommand.Execute(null);
                scheduler.AdvanceByMs(15000);
            });

            Assert.Equal(1, thrown.Count);
        }

        [Fact]
        public void LoadPlaylistCommandSmokeTest()
        {
            var songs = Helpers.SetupSongs(2);
            var playlist = new Playlist("Playlist 1", songs, 1);

            var messenger = CreateDefaultPlaylistMessenger();
            messenger.Setup(x => x.GetCurrentPlaylistAsync()).Returns(playlist.ToTaskResult);

            var vm = new PlaylistViewModel();

            vm.LoadPlaylistCommand.Execute(null);

            Assert.Equal(playlist.Songs.Count, vm.Entries.Count);
        }

        [Fact]
        public void PlaylistChangeUpdatesPlaylist()
        {
            var songs = Helpers.SetupSongs(2);
            var playlist = new Playlist("Playlist 1", songs, 1);

            var messenger = CreateDefaultPlaylistMessenger();
            messenger.SetupGet(x => x.PlaylistChanged).Returns(Observable.Return(playlist));

            var vm = new PlaylistViewModel();

            Assert.True(vm.Entries[playlist.CurrentIndex.Value].IsPlaying);
            //Assert.Equal(playlist.Name, vm.Name);
            Assert.Equal(playlist.Songs.Count, vm.Entries.Count);
        }

        [Fact]
        public void PlayNextSongCommandCanExecuteIsFalseForEmptyPlaylist()
        {
            var playlist = new Playlist("Playlist 1", new List<Song>(), 0);

            var messenger = CreateDefaultPlaylistMessenger();
            messenger.Setup(x => x.GetCurrentPlaylistAsync()).Returns(playlist.ToTaskResult());
            messenger.Setup(x => x.PlayNextSongAsync()).Returns(new ResponseInfo(200, "Ok").ToTaskResult());

            var vm = new PlaylistViewModel();
            vm.LoadPlaylistCommand.Execute(null);

            Assert.False(vm.PlayNextSongCommand.CanExecute(null));
        }

        [Fact]
        public void PlayNextSongCommandSmokeTest()
        {
            var songs = Helpers.SetupSongs(2);
            var playlist = new Playlist("Playlist 1", songs, 0);

            var messenger = CreateDefaultPlaylistMessenger();
            messenger.Setup(x => x.GetCurrentPlaylistAsync()).Returns(playlist.ToTaskResult());
            messenger.Setup(x => x.PlayNextSongAsync()).Returns(new ResponseInfo(200, "Ok").ToTaskResult());

            var playlists = new Subject<Playlist>();
            messenger.Setup(x => x.PlaylistChanged).Returns(playlists);

            var vm = new PlaylistViewModel();
            vm.LoadPlaylistCommand.Execute(null);

            var canExecute = vm.PlayNextSongCommand.CanExecuteObservable.CreateCollection();

            vm.PlayNextSongCommand.Execute(null);

            playlists.OnNext(new Playlist("Playlist 1", songs, 1));
            playlists.OnNext(new Playlist("Playlist 1", songs, 0));
            playlists.OnNext(new Playlist("Playlist 1", songs, null));

            messenger.Verify(x => x.PlayNextSongAsync(), Times.Once);
            Assert.Equal(new[] { true, false, true, false, true, false }, canExecute);
        }

        [Fact]
        public void PlayPauseCommandSmokeTest()
        {
            var songs = Helpers.SetupSongs(2);
            var playlist = new Playlist("Playlist 1", songs, 0);

            var messenger = CreateDefaultPlaylistMessenger();
            messenger.Setup(x => x.GetCurrentPlaylistAsync()).Returns(playlist.ToTaskResult());
            messenger.Setup(x => x.ContinueSongAsync()).Returns(new ResponseInfo(200, "Ok").ToTaskResult());
            messenger.Setup(x => x.PauseSongAsync()).Returns(new ResponseInfo(200, "Ok").ToTaskResult());

            var playbackState = new Subject<PlaybackState>();
            messenger.SetupGet(x => x.PlaybackStateChanged).Returns(playbackState);

            var vm = new PlaylistViewModel();
            vm.LoadPlaylistCommand.Execute(null);

            var canExecute = vm.PlayPauseCommand.CanExecuteObservable.CreateCollection();

            playbackState.OnNext(PlaybackState.Paused);

            vm.PlayPauseCommand.Execute(null);
            messenger.Verify(x => x.ContinueSongAsync(), Times.Once);

            playbackState.OnNext(PlaybackState.Playing);

            vm.PlayPauseCommand.Execute(null);
            messenger.Verify(x => x.PauseSongAsync(), Times.Once);

            playbackState.OnNext(PlaybackState.Paused);

            Assert.Equal(new[] { false, true, false, true, false, true }, canExecute);
        }

        [Fact]
        public void PlayPlaylistSongCommandSmokeTest()
        {
            var songs = Helpers.SetupSongs(2);
            var playlist = new Playlist("Playlist 1", songs, 0);

            var messenger = CreateDefaultPlaylistMessenger();
            messenger.Setup(x => x.GetCurrentPlaylistAsync()).Returns(playlist.ToTaskResult());
            messenger.Setup(x => x.PlayPlaylistSongAsync(It.Is<Guid>(y => y == songs[1].Guid)))
                .Returns(new ResponseInfo(200, "Ok").ToTaskResult());

            var vm = new PlaylistViewModel();

            var coll = vm.Message.CreateCollection();

            vm.LoadPlaylistCommand.Execute(null);

            vm.PlayPlaylistSongCommand.Execute(1);

            messenger.Verify();
            messenger.Verify(x => x.PlayPlaylistSongAsync(It.IsAny<Guid>()), Times.Once);
            Assert.Equal(1, coll.Count);
        }

        [Fact]
        public void PlayPreviousSongCommandCanExecuteIsFalseForEmptyPlaylist()
        {
            var playlist = new Playlist("Playlist 1", new List<Song>(), 0);

            var messenger = CreateDefaultPlaylistMessenger();
            messenger.Setup(x => x.GetCurrentPlaylistAsync()).Returns(playlist.ToTaskResult());
            messenger.Setup(x => x.PlayNextSongAsync()).Returns(new ResponseInfo(200, "Ok").ToTaskResult());

            var vm = new PlaylistViewModel();
            vm.LoadPlaylistCommand.Execute(null);

            Assert.False(vm.PlayPreviousSongCommand.CanExecute(null));
        }

        [Fact]
        public void PlayPreviousSongCommandSmokeTest()
        {
            var songs = Helpers.SetupSongs(2);
            var playlist = new Playlist("Playlist 1", songs, 1);

            var messenger = CreateDefaultPlaylistMessenger();
            messenger.Setup(x => x.GetCurrentPlaylistAsync()).Returns(playlist.ToTaskResult());
            messenger.Setup(x => x.PlayPreviousSongAsync()).Returns(new ResponseInfo(200, "Ok").ToTaskResult());

            var playlists = new Subject<Playlist>();
            messenger.SetupGet(x => x.PlaylistChanged).Returns(playlists);

            var vm = new PlaylistViewModel();
            vm.LoadPlaylistCommand.Execute(null);

            var canExecute = vm.PlayPreviousSongCommand.CanExecuteObservable.CreateCollection();

            vm.PlayPreviousSongCommand.Execute(null);

            playlists.OnNext(new Playlist("Playlist 1", songs, 0));
            playlists.OnNext(new Playlist("Playlist 1", songs, 1));
            playlists.OnNext(new Playlist("Playlist 1", songs, null));

            messenger.Verify(x => x.PlayPreviousSongAsync(), Times.Once);
            Assert.Equal(new[] { true, false, true, false, true, false }, canExecute);
        }

        private static Mock<INetworkMessenger> CreateDefaultPlaylistMessenger()
        {
            var messenger = new Mock<INetworkMessenger>();
            messenger.SetupGet(x => x.PlaybackStateChanged).Returns(Observable.Never<PlaybackState>());
            messenger.SetupGet(x => x.PlaylistChanged).Returns(Observable.Never<Playlist>());
            messenger.SetupGet(x => x.AccessPermission).Returns(Observable.Return(AccessPermission.Admin));
            messenger.Setup(x => x.GetPlaybackStateAsync()).Returns(PlaybackState.None.ToTaskResult());

            NetworkMessenger.Override(messenger.Object);

            return messenger;
        }
    }
}