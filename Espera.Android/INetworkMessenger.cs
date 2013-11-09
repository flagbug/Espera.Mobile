using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive;
using System.Threading.Tasks;

namespace Espera.Android
{
    public interface INetworkMessenger
    {
        IObservable<Unit> Disconnected { get; }

        IObservable<bool> IsConnected { get; }

        IObservable<PlaybackState> PlaybackStateChanged { get; }

        IObservable<Playlist> PlaylistChanged { get; }

        IObservable<int?> PlaylistIndexChanged { get; }

        Task<Tuple<int, string>> AddSongToPlaylist(Guid songGuid);

        Task ConnectAsync(IPAddress address, int port);

        Task<Tuple<int, string>> ContinueSong();

        void Disconnect();

        void Dispose();

        Task<Playlist> GetCurrentPlaylist();

        Task<PlaybackState> GetPlaybackSate();

        Task<IReadOnlyList<Song>> GetSongsAsync();

        Task<Tuple<int, string>> PauseSong();

        Task<Tuple<int, string>> PlayNextSong();

        Task<Tuple<int, string>> PlayPlaylistSong(Guid guid);

        Task<Tuple<int, string>> PlayPreviousSong();

        Task<Tuple<int, string>> PlaySongs(IEnumerable<Guid> guids);

        Task<Tuple<int, string>> RemovePlaylistSong(Guid guid);
    }
}