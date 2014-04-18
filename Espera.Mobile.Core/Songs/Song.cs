namespace Espera.Mobile.Core.Songs
{
    public abstract class Song
    {
        protected Song(string title, string artist, string album)
        {
            this.Title = title;
            this.Artist = artist;
            this.Album = album;
        }

        public string Album { get; private set; }

        public string Artist { get; private set; }

        public string Title { get; private set; }
    }
}