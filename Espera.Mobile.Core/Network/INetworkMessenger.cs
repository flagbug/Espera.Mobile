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

        Task<ResponseInfo> AddSongToPlaylistAsync(Guid songGuid);

        Task<ConnectionResultContainer> ConnectAsync(string address, int port, Guid deviceId, string password);

        Task<ResponseInfo> ContinueSongAsync();

        void Disconnect();

        IObservable<string> DiscoverServerAsync(string localAddress, int port);

        Task<NetworkPlaylist> GetCurrentPlaylistAsync();

        Task<IReadOnlyList<NetworkSong>> GetSongsAsync();

        Task<IReadOnlyList<NetworkSong>> GetSoundCloudSongsAsync(string searchTerm);

        Task<float> GetVolume();

        Task<ResponseInfo> MovePlaylistSongDownAsync(Guid entryGuid);

        Task<ResponseInfo> MovePlaylistSongUpAsync(Guid entryGuid);

        Task<ResponseInfo> PauseSongAsync();

        Task<ResponseInfo> PlayNextSongAsync();

        Task<ResponseInfo> PlayPlaylistSongAsync(Guid entryGuid);

        Task<ResponseInfo> PlayPreviousSongAsync();

        Task<ResponseInfo> PlaySongsAsync(IEnumerable<Guid> guids);

        Task<FileTransferStatus> QueueRemoteSong(LocalSong songMetadata, byte[] data);

        Task<ResponseInfo> RemovePlaylistSongAsync(Guid entryGuid);

        Task<ResponseInfo> SetCurrentTime(TimeSpan time);

        Task<ResponseInfo> SetVolume(float volume);

        Task<ResponseInfo> VoteAsync(Guid entryGuid);
    }
}