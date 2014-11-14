using Akavache;
using Espera.Network;
using ReactiveMarrow;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Espera.Mobile.Core.Network
{
    /// <summary>
    /// A network messenger that returns virtual data, so we don't have to connect to a real host.
    /// </summary>
    public class VirtualNetworkMessenger : ReactiveObject, INetworkMessenger
    {
        private readonly InMemoryBlobCache cache;
        private readonly Subject<NetworkPlaybackState> playbackState;
        private readonly Subject<TimeSpan> playbackTime;
        private bool isConnected;

        public VirtualNetworkMessenger()
        {
            this.playbackState = new Subject<NetworkPlaybackState>();
            this.playbackTime = new Subject<TimeSpan>();
            this.cache = new InMemoryBlobCache();

            this.Disconnected = this.WhenAnyValue(x => x.IsConnected)
                .Skip(1)
                .Where(x => !x)
                .ToUnit();
        }

        public NetworkAccessPermission AccessPermission
        {
            get { return NetworkAccessPermission.Admin; }
        }

        public IObservable<Unit> Disconnected { get; }

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
            get { return this.playbackState.AsObservable(); }
        }

        public IObservable<TimeSpan> PlaybackTimeChanged
        {
            get { return this.playbackTime.AsObservable(); }
        }

        public IObservable<NetworkPlaylist> PlaylistChanged
        {
            get { return Observable.Never<NetworkPlaylist>(); }
        }

        public Task AddSongToPlaylistAsync(Guid songGuid) => Success();

        public Task<ConnectionResultContainer> ConnectAsync(string address, int port, Guid deviceId, string password)
        {
            this.IsConnected = true;

            return Task.FromResult(new ConnectionResultContainer(ConnectionResult.Successful, NetworkAccessPermission.Admin, new Version("99.99.99")));
        }

        public Task ContinueSongAsync()
        {
            this.playbackState.OnNext(NetworkPlaybackState.Playing);

            return Success();
        }

        public void Disconnect() => this.IsConnected = false;

        public IObservable<string> DiscoverServerAsync(string localAddress, int port) => Observable.Return("192.169.1.10");

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
            return this.cache.GetOrCreateObject("songs", () =>
            {
                var random = new Random();

                List<NetworkSong> songs = Enumerable.Range(0, 9)
                    .Select(_ =>
                        new NetworkSong
                        {
                            Guid = Guid.NewGuid(),
                            Source = NetworkSongSource.Local,
                            Duration = GetRandomSongTime(random)
                        })
                    .ToList();

                var attilaSongs = songs.Take(3).ToList();

                foreach (NetworkSong attilaSong in attilaSongs)
                {
                    attilaSong.Album = "About That Life";
                    attilaSong.Artist = "Attila";
                    attilaSong.Genre = "Partycore";
                }

                attilaSongs[0].Title = "Backtalk";
                attilaSongs[1].Title = "About That Life";
                attilaSongs[2].Title = "Shots For The Boys";

                var peripherySongs = songs.Skip(3).Take(3).ToList();

                foreach (NetworkSong peripherySong in peripherySongs)
                {
                    peripherySong.Album = "Clear EP";
                    peripherySong.Artist = "Periphery";
                    peripherySong.Genre = "Progressive Metal";
                }

                peripherySongs[0].Title = "Overture";
                peripherySongs[1].Title = "Zero: Misha";
                peripherySongs[2].Title = "Pale Aura: Mark";

                var curesickSongs = songs.Skip(6).Take(3).ToList();

                foreach (NetworkSong curesickSong in curesickSongs)
                {
                    curesickSong.Album = "Dead End";
                    curesickSong.Artist = "CureSick";
                    curesickSong.Genre = "Alternative Melodic Metal";
                }

                curesickSongs[0].Title = "Shades";
                curesickSongs[1].Title = "December Morning";
                curesickSongs[2].Title = "Till The End";

                return (IReadOnlyList<NetworkSong>)songs;
            }).ToTask();
        }

        public Task<IReadOnlyList<NetworkSong>> GetSoundCloudSongsAsync(string searchTerm) => Task.FromResult((IReadOnlyList<NetworkSong>)new List<NetworkSong>());

        public Task<float> GetVolume() => Task.FromResult(1.0f);

        public Task<IReadOnlyList<NetworkSong>> GetYoutubeSongsAsync(string searchTerm) => Task.FromResult((IReadOnlyList<NetworkSong>)new List<NetworkSong>());

        public Task MovePlaylistSongDownAsync(Guid entryGuid) => Success();

        public Task MovePlaylistSongUpAsync(Guid entryGuid) => Success();

        public Task PauseSongAsync()
        {
            this.playbackState.OnNext(NetworkPlaybackState.Paused);

            return Success();
        }

        public Task PlayNextSongAsync()
        {
            this.playbackState.OnNext(NetworkPlaybackState.Playing);

            return Success();
        }

        public Task PlayPlaylistSongAsync(Guid entryGuid)
        {
            this.playbackState.OnNext(NetworkPlaybackState.Playing);

            return Success();
        }

        public Task PlayPreviousSongAsync()
        {
            this.playbackState.OnNext(NetworkPlaybackState.Playing);

            return Success();
        }

        public Task PlaySongsAsync(IEnumerable<Guid> guids)
        {
            this.playbackState.OnNext(NetworkPlaybackState.Playing);

            return Success();
        }

        public async Task<FileTransferStatus> QueueRemoteSong(LocalSong songMetadata, byte[] data) => new FileTransferStatus(await Success(), Observable.Never<int>());

        public Task RemovePlaylistSongAsync(Guid entryGuid) => Success();

        public Task SetCurrentTime(TimeSpan time)
        {
            this.playbackTime.OnNext(time);

            return Success();
        }

        public Task SetVolume(float volume) => Success();

        public Task ToggleVideoPlayer() => Success();

        public Task VoteAsync(Guid entryGuid) => Success();

        private static TimeSpan GetRandomSongTime(Random random)
        {
            int lowerTimeBound = TimeSpan.FromMinutes(1).Milliseconds;
            int upperTimeBound = TimeSpan.FromMinutes(3).Milliseconds;

            int theRandom = random.Next(lowerTimeBound, upperTimeBound);

            return TimeSpan.FromMilliseconds(theRandom);
        }

        private static Task<ResponseInfo> Success() => Task.FromResult(new ResponseInfo { RequestId = Guid.NewGuid(), Status = ResponseStatus.Success });
    }
}