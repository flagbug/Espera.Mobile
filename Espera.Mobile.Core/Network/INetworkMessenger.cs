using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reactive;
using System.Threading.Tasks;
using Espera.Network;

namespace Espera.Mobile.Core.Network
{
    public interface INetworkMessenger : IDisposable, INotifyPropertyChanged
    {
        NetworkAccessPermission AccessPermission { get; }

        IObservable<Unit> Disconnected { get; }

        GuestSystemInfo GuestSystemInfo { get; }

        bool IsConnected { get; }

        IObservable<NetworkPlaybackState> PlaybackStateChanged { get; }

        IObservable<TimeSpan> PlaybackTimeChanged { get; }

        IObservable<NetworkPlaylist> PlaylistChanged { get; }

        Task AddSongToPlaylistAsync(Guid songGuid);

        Task<ConnectionResultContainer> ConnectAsync(string address, int port, Guid deviceId, string password);

        Task ContinueSongAsync();

        void Disconnect();

        IObservable<string> DiscoverServerAsync(string localAddress, int port);

        Task<NetworkPlaylist> GetCurrentPlaylistAsync();

        Task<IReadOnlyList<NetworkSong>> GetSongsAsync();

        Task<IReadOnlyList<NetworkSong>> GetSoundCloudSongsAsync(string searchTerm);

        Task<float> GetVolume();

        Task<IReadOnlyList<NetworkSong>> GetYoutubeSongsAsync(string searchTerm);

        Task MovePlaylistSongDownAsync(Guid entryGuid);

        Task MovePlaylistSongUpAsync(Guid entryGuid);

        Task PauseSongAsync();

        Task PlayNextSongAsync();

        Task PlayPlaylistSongAsync(Guid entryGuid);

        Task PlayPreviousSongAsync();

        Task PlaySongsAsync(IEnumerable<Guid> guids);

        Task<FileTransferStatus> QueueRemoteSong(LocalSong songMetadata, byte[] data);

        Task RemovePlaylistSongAsync(Guid entryGuid);

        Task SetCurrentTime(TimeSpan time);

        Task SetVolume(float volume);

        Task ToggleVideoPlayer();

        Task VoteAsync(Guid entryGuid);
    }
}