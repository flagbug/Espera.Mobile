using System;

namespace Espera.Mobile.Core.Songs
{
    public class LocalSong : Song
    {
        public LocalSong(string title, string artist, string album, string genre, TimeSpan duration, string path)
            : base(title, artist, album, genre, duration)
        {
            this.Path = path;
        }

        public string Path { get; private set; }
    }
}