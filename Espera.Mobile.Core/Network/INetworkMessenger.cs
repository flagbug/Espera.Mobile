using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading.Tasks;
using Espera.Mobile.Core.Analytics;
using Espera.Mobile.Core.Songs;
using Espera.Network;

namespace Espera.Mobile.Core.Network
{
    public interface INetworkMessenger
    {
        IObservable<NetworkAccessPermission> AccessPermission { get; }

        IObservable<Unit> Disconnected { get; }

        IObservable<bool> IsConnected { get; }

        IObservable<NetworkPlaybackState> PlaybackStateChanged { get; }

        IObservable<TimeSpan> PlaybackTimeChanged { get; }

        IObservable<NetworkPlaylist> PlaylistChanged { get; }

        IObservable<int?> RemainingVotesChanged { get; }

        Task<ResponseInfo> AddSongToPlaylistAsync(Guid songGuid);

        Task<Tuple<ResponseStatus, ConnectionInfo>> ConnectAsync(string address, int port, Guid deviceId, string password);

        Task<ResponseInfo> ContinueSongAsync();

        void Disconnect();

        Task<string> DiscoverServerAsync(string localAddress, int port);

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

        Task<FileTransferStatus> QueueRemoteSong(LocalSong songMetadata, byte[] data);

        void RegisterAnalytics(IAnalytics analytics);

        Task<ResponseInfo> RemovePlaylistSongAsync(Guid entryGuid);

        Task<ResponseInfo> SetCurrentTime(TimeSpan time);

        Task<ResponseInfo> VoteAsync(Guid entryGuid);
    }
}