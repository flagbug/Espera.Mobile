using System;

namespace Espera.Android
{
    public class Song
    {
        public Song(string artist, string title, string genre, string album)
        {
            if (artist == null)
                throw new ArgumentNullException("artist");

            if (title == null)
                throw new ArgumentNullException("title");

            if (genre == null)
                throw new ArgumentNullException("genre");

            if (album == null)
                throw new ArgumentNullException("album");

            this.Artist = artist;
            this.Title = title;
            this.Genre = genre;
            this.Album = album;
        }

        public string Album { get; private set; }

        public string Artist { get; private set; }

        public string Genre { get; private set; }

        public string Title { get; private set; }
    }
}