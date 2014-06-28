using System;

namespace Espera.Mobile.Core.Songs
{
    public class LocalSong : Song
    {
        public LocalSong(string title, string artist, string album, string genre, TimeSpan duration, string path, Func<byte[]> dataFunc)
            : base(title, artist, album, genre, duration)
        {
            if (dataFunc == null)
                throw new ArgumentNullException("dataFunc");

            this.Path = path;
            this.Data = dataFunc;
        }

        public Func<byte[]> Data { get; private set; }

        public string Path { get; private set; }
    }
}