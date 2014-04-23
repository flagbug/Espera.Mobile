using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive;
using System.Threading.Tasks;
using Espera.Mobile.Core.Analytics;
using Espera.Network;

namespace Espera.Mobile.Core.Network
{
    public interface INetworkMessenger
    {
        IObservable<NetworkAccessPermission> AccessPermission { get; }

        IObservable<Unit> Disconnected { get; }

        IObservable<bool> IsConnected { get; }

        IObservable<NetworkPlaybackState> PlaybackStateChanged { get; }

        IObservable<NetworkPlaylist> PlaylistChanged { get; }

        IObservable<int?> RemainingVotesChanged { get; }

        Task<ResponseInfo> AddSongToPlaylistAsync(Guid songGuid);

        Task<Tuple<ResponseStatus, ConnectionInfo>> ConnectAsync(IPAddress address, int port, Guid deviceId, string password);

        Task<ResponseInfo> ContinueSongAsync();

        void Disconnect();

        void Dispose();

        Task<NetworkPlaylist> GetCurrentPlaylistAsync();

        Task<IReadOnlyList<NetworkSong>> GetSongsAsync();

        Task<ResponseInfo> MovePlaylistSongDownAsync(Guid entryGuid);

        Task<ResponseInfo> MovePlaylistSongUpAsync(Guid entryGuid);

        Task<ResponseInfo> PauseSongAsync();

        Task<ResponseInfo> PlayNextSongAsync();

        Task<ResponseInfo> PlayPlaylistSongAsync(Guid entryGuid);

        Task<ResponseInfo> PlayPreviousSongAsync();

        Task<ResponseInfo> PlaySongsAsync(IEnumerable<Guid> guids);

        Task<FileTransferStatus> QueueRemoteSong(byte[] data);

        void RegisterAnalytics(IAnalytics analytics);

        Task<ResponseInfo> RemovePlaylistSongAsync(Guid entryGuid);

        Task<ResponseInfo> VoteAsync(Guid entryGuid);
    }
}