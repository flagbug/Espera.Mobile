using System;

namespace Espera.Mobile.Core.Songs
{
    public class LocalSong : Song
    {
        public LocalSong(string title, string artist, string album, string genre, TimeSpan duration, int trackNumber, string path)
            : base(title, artist, album, genre, duration, trackNumber)
        {
            this.Path = path;
        }

        public string Path { get; private set; }
    }
}