namespace Espera.Mobile.Core.Songs
{
    public class LocalSong : Song
    {
        public LocalSong(string title, string artist, string album, string path)
            : base(title, artist, album)
        {
            this.Path = path;
        }

        public string Path { get; private set; }
    }
}