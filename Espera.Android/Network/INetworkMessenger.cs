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

        IObservable<int> RemainingVotesChanged { get; }

        Task<ResponseInfo> AddSongToPlaylist(Guid songGuid);

        Task<ConnectionInfo> ConnectAsync(IPAddress address, int port, string password);

        Task<ResponseInfo> ContinueSong();

        void Disconnect();

        void Dispose();

        Task<Playlist> GetCurrentPlaylist();

        Task<PlaybackState> GetPlaybackState();

        Task<IReadOnlyList<Song>> GetSongsAsync();

        Task<ResponseInfo> MovePlaylistSongDown(Guid entryGuid);

        Task<ResponseInfo> MovePlaylistSongUp(Guid entryGuid);

        Task<ResponseInfo> PauseSong();

        Task<ResponseInfo> PlayNextSong();

        Task<ResponseInfo> PlayPlaylistSong(Guid entryGuid);

        Task<ResponseInfo> PlayPreviousSong();

        Task<ResponseInfo> PlaySongs(IEnumerable<Guid> guids);

        Task<ResponseInfo> RemovePlaylistSong(Guid entryGuid);

        Task<ResponseInfo> Vote(Guid entryGuid);
    }
}