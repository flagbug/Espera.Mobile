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

        /// <summary>
        /// Serialization constructor
        /// </summary>
        protected Song()
        { }

        public string Album { get; set; }

        public string Artist { get; set; }

        public TimeSpan Duration { get; set; }

        public string Genre { get; set; }

        public string Title { get; set; }
    }
}