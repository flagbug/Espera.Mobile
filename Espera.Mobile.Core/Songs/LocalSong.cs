namespace Espera.Mobile.Core.Songs
{
    public class LocalSong : Song
    {
        public LocalSong(string title, string artist, string album, string id)
            : base(title, artist, album)
        {
            this.Id = id;
        }

        public string Id { get; private set; }
    }
}