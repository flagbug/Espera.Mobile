using System;

namespace Espera.Mobile.Core.Songs
{
    public abstract class Song
    {
        protected Song(string title, string artist, string album, string genre, TimeSpan duration)
        {
            this.Title = title;
            this.Artist = artist;
            this.Album = album;
            this.Genre = genre;
            this.Duration = duration;
        }

        public string Album { get; private set; }

        public string Artist { get; private set; }

        public TimeSpan Duration { get; private set; }

        public string Genre { get; private set; }

        public string Title { get; private set; }
    }
}