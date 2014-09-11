using System;
using Espera.Network;

namespace Espera.Mobile.Core
{
    public class LocalSong : NetworkSong
    {
        public LocalSong(string title, string artist, string album, string genre, TimeSpan duration, int trackNumber, string path)
        {
            this.Title = title;
            this.Artist = artist;
            this.Album = album;
            this.Genre = genre;
            this.Duration = duration;
            this.TrackNumber = trackNumber;
            this.Path = path;
        }

        public string Path { get; private set; }
    }
}