using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Espera.Network;
using ReactiveMarrow;
using ReactiveUI;

namespace Espera.Mobile.Core.Network
{
    /// <summary>
    /// A network messenger that returns virtual data, so we don't have to connect to a real host.
    /// </summary>
    public class VirtualNetworkMessenger : ReactiveObject, INetworkMessenger
    {
        private bool isConnected;

        public VirtualNetworkMessenger()
        {
            this.Disconnected = this.WhenAnyValue(x => x.IsConnected)
                .Skip(1)
                .Where(x => !x)
                .ToUnit();
        }

        public NetworkAccessPermission AccessPermission
        {
            get { return NetworkAccessPermission.Admin; }
        }

        public IObservable<Unit> Disconnected { get; private set; }

        public GuestSystemInfo GuestSystemInfo
        {
            get
            {
                return new GuestSystemInfo
                {
                    IsEnabled = true,
                    RemainingVotes = 3
                };
            }
        }

        public bool IsConnected
        {
            get { return this.isConnected; }
            private set { this.RaiseAndSetIfChanged(ref this.isConnected, value); }
        }

        public IObservable<NetworkPlaybackState> PlaybackStateChanged
        {
            get { return Observable.Never<NetworkPlaybackState>(); }
        }

        public IObservable<TimeSpan> PlaybackTimeChanged
        {
            get { return Observable.Never<TimeSpan>(); }
        }

        public IObservable<NetworkPlaylist> PlaylistChanged
        {
            get { return Observable.Never<NetworkPlaylist>(); }
        }

        public Task<ResponseInfo> AddSongToPlaylistAsync(Guid songGuid)
        {
            return this.Success();
        }

        public Task<ConnectionResultContainer> ConnectAsync(string address, int port, Guid deviceId, string password)
        {
            this.IsConnected = true;

            return Task.FromResult(new ConnectionResultContainer(ConnectionResult.Successful, NetworkAccessPermission.Admin, new Version("99.99.99")));
        }

        public Task<ResponseInfo> ContinueSongAsync()
        {
            return this.Success();
        }

        public void Disconnect()
        {
            this.IsConnected = false;
        }

        public IObservable<string> DiscoverServerAsync(string localAddress, int port)
        {
            return Observable.Return("192.169.1.10");
        }

        public void Dispose()
        { }

        public async Task<NetworkPlaylist> GetCurrentPlaylistAsync()
        {
            var playlist = new NetworkPlaylist
            {
                CurrentIndex = 4,
                CurrentTime = TimeSpan.FromSeconds(5),
                Name = "Party Playlist",
                PlaybackState = NetworkPlaybackState.Playing,
                Songs = new ReadOnlyCollection<NetworkSong>((await this.GetSongsAsync()).ToList()),
                TotalTime = TimeSpan.FromMinutes(3)
            };

            return playlist;
        }

        public Task<IReadOnlyList<NetworkSong>> GetSongsAsync()
        {
            List<NetworkSong> songs = Enumerable.Range(0, 10)
                .Select(_ =>
                    new NetworkSong
                    {
                        Album = "About That Life",
                        Artist = "Attila",
                        Genre = "Partycore",
                        Guid = Guid.NewGuid(),
                        Source = NetworkSongSource.Local,
                        Duration = TimeSpan.FromMinutes(3)
                    })
                .ToList();

            songs[0].Title = "Hellraiser";
            songs[1].Title = "Rageaholics";
            songs[2].Title = "Backtalk";
            songs[3].Title = "Leave A Message";
            songs[4].Title = "About That Life";
            songs[5].Title = "Thug Life";
            songs[6].Title = "Break Shit";
            songs[7].Title = "Unfogivable";
            songs[8].Title = "Shots For The Boys";
            songs[9].Title = "Party With The Devil";

            return Task.FromResult((IReadOnlyList<NetworkSong>)songs);
        }

        public Task<IReadOnlyList<NetworkSong>> GetSoundCloudSongsAsync(string searchTerm)
        {
            return Task.FromResult((IReadOnlyList<NetworkSong>)new List<NetworkSong>());
        }

        public Task<float> GetVolume()
        {
            return Task.FromResult(1.0f);
        }

        public Task<IReadOnlyList<NetworkSong>> GetYoutubeSongsAsync(string searchTerm)
        {
            return Task.FromResult((IReadOnlyList<NetworkSong>)new List<NetworkSong>());
        }

        public Task<ResponseInfo> MovePlaylistSongDownAsync(Guid entryGuid)
        {
            return this.Success();
        }

        public Task<ResponseInfo> MovePlaylistSongUpAsync(Guid entryGuid)
        {
            return this.Success();
        }

        public Task<ResponseInfo> PauseSongAsync()
        {
            return this.Success();
        }

        public Task<ResponseInfo> PlayNextSongAsync()
        {
            return this.Success();
        }

        public Task<ResponseInfo> PlayPlaylistSongAsync(Guid entryGuid)
        {
            return this.Success();
        }

        public Task<ResponseInfo> PlayPreviousSongAsync()
        {
            return this.Success();
        }

        public Task<ResponseInfo> PlaySongsAsync(IEnumerable<Guid> guids)
        {
            return this.Success();
        }

        public async Task<FileTransferStatus> QueueRemoteSong(LocalSong songMetadata, byte[] data)
        {
            return new FileTransferStatus(await this.Success(), Observable.Never<int>());
        }

        public Task<ResponseInfo> RemovePlaylistSongAsync(Guid entryGuid)
        {
            return this.Success();
        }

        public Task<ResponseInfo> SetCurrentTime(TimeSpan time)
        {
            return this.Success();
        }

        public Task<ResponseInfo> SetVolume(float volume)
        {
            return this.Success();
        }

        public Task<ResponseInfo> ToggleVideoPlayer()
        {
            return this.Success();
        }

        public Task<ResponseInfo> VoteAsync(Guid entryGuid)
        {
            return this.Success();
        }

        private Task<ResponseInfo> Success()
        {
            return Task.FromResult(new ResponseInfo { RequestId = Guid.NewGuid(), Status = ResponseStatus.Success });
        }
    }
}