using System.Collections.Generic;

namespace Espera.Android
{
    internal class Playlist
    {
        public Playlist(string name, IReadOnlyList<Song> songs)
        {
            this.Name = name;
            this.Songs = songs;
        }

        public string Name { get; private set; }

        public IReadOnlyList<Song> Songs { get; private set; }
    }
}