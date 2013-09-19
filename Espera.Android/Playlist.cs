using System.Collections.Generic;

namespace Espera.Android
{
    public class Playlist
    {
        public Playlist(string name, IReadOnlyList<Song> songs, int? currentIndex)
        {
            this.Name = name;
            this.Songs = songs;
            this.CurrentIndex = currentIndex;
        }

        public int? CurrentIndex { get; private set; }

        public string Name { get; private set; }

        public IReadOnlyList<Song> Songs { get; private set; }
    }
}