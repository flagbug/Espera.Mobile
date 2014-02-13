using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Espera.Android
{
    public class Playlist
    {
        public Playlist(string name, IReadOnlyList<Song> songs, int? currentIndex, int remainingVotes = 0)
        {
            this.Name = name;
            this.Songs = songs;
            this.CurrentIndex = currentIndex;
            this.RemainingVotes = remainingVotes;
        }

        public int? CurrentIndex { get; private set; }

        public string Name { get; private set; }

        public int RemainingVotes { get; private set; }

        public IReadOnlyList<Song> Songs { get; private set; }

        public static Playlist Deserialize(JToken json)
        {
            string name = json["name"].ToString();

            List<Song> songs = json["songs"]
                .Select(x =>
                    new Song(x["artist"].ToString(), x["title"].ToString(), String.Empty,
                        String.Empty, TimeSpan.Zero, Guid.Parse(x["guid"].ToString()),
                        x["source"].ToString() == "local" ? SongSource.Local : SongSource.Youtube))
                .ToList();

            int? currentIndex = json["current"].ToObject<int?>();
            int remainingVotes = json["remainingVotes"].ToObject<int>();

            return new Playlist(name, songs, currentIndex, remainingVotes);
        }
    }
}