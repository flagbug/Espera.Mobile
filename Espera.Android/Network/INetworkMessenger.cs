using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive;
using System.Threading.Tasks;

namespace Espera.Android.Network
{
    public interface INetworkMessenger
    {
        IObservable<AccessPermission> AccessPermission { get; }

        IObservable<Unit> Disconnected { get; }

        IObservable<bool> IsConnected { get; }

        IObservable<PlaybackState> PlaybackStateChanged { get; }

        IObservable<Playlist> PlaylistChanged { get; }

        IObservable<int?> RemainingVotesChanged { get; }

        Task<ResponseInfo> AddSongToPlaylistAsync(Guid songGuid);

        Task<ConnectionInfo> ConnectAsync(IPAddress address, int port, string password);

        Task<ResponseInfo> ContinueSongAsync();

        void Disconnect();

        void Dispose();

        Task<Playlist> GetCurrentPlaylistAsync();

        Task<PlaybackState> GetPlaybackStateAsync();

        Task<IReadOnlyList<Song>> GetSongsAsync();

        Task<ResponseInfo> MovePlaylistSongDownAsync(Guid entryGuid);

        Task<ResponseInfo> MovePlaylistSongUpAsync(Guid entryGuid);

        Task<ResponseInfo> PauseSongAsync();

        Task<ResponseInfo> PlayNextSongAsync();

        Task<ResponseInfo> PlayPlaylistSongAsync(Guid entryGuid);

        Task<ResponseInfo> PlayPreviousSongAsync();

        Task<ResponseInfo> PlaySongsAsync(IEnumerable<Guid> guids);

        Task<ResponseInfo> RemovePlaylistSongAsync(Guid entryGuid);

        Task<ResponseInfo> VoteAsync(Guid entryGuid);
    }
}