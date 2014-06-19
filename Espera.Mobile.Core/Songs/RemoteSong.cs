using System;
using Espera.Network;

namespace Espera.Mobile.Core.Songs
{
    public class RemoteSong : Song
    {
        private RemoteSong(string title, string artist, string album, string genre, TimeSpan duration, Guid guid, NetworkSongSource source)
            : base(title, artist, album, genre, duration)
        {
            this.Guid = guid;
            this.Source = source;
        }

        public Guid Guid { get; private set; }

        public NetworkSongSource Source { get; private set; }

        public static RemoteSong FromNetworkSong(NetworkSong networkSong)
        {
            return new RemoteSong(networkSong.Title, networkSong.Artist, networkSong.Album, networkSong.Genre, networkSong.Duration, networkSong.Guid, networkSong.Source);
        }
    }
}