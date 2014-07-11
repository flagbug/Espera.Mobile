using System;
using Espera.Network;

namespace Espera.Mobile.Core.Songs
{
    public class RemoteSong : Song
    {
        /// <summary>
        /// Serialization constructor
        /// </summary>
        public RemoteSong()
        { }

        private RemoteSong(string title, string artist, string album, string genre, TimeSpan duration, int trackNumber, Guid guid, NetworkSongSource source)
            : base(title, artist, album, genre, duration, trackNumber)
        {
            this.Guid = guid;
            this.Source = source;
        }

        public Guid Guid { get; set; }

        public NetworkSongSource Source { get; set; }

        public static RemoteSong FromNetworkSong(NetworkSong networkSong)
        {
            return new RemoteSong(networkSong.Title, networkSong.Artist, networkSong.Album, networkSong.Genre, networkSong.Duration, networkSong.TrackNumber, networkSong.Guid, networkSong.Source);
        }
    }
}