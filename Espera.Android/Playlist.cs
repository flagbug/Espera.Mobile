using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Espera.Android
{
    public class Playlist
    {
        private readonly Subject<Unit> changed;

        private int? currentIndex;

        public Playlist(string name, IReadOnlyList<Song> songs, int? currentIndex)
        {
            this.Name = name;
            this.Songs = songs;
            this.currentIndex = currentIndex;

            this.changed = new Subject<Unit>();
        }

        public IObservable<Unit> Changed
        {
            get { return this.changed.AsObservable(); }
        }

        public int? CurrentIndex
        {
            get { return this.currentIndex; }
            set
            {
                this.currentIndex = value;
                this.changed.OnNext(Unit.Default);
            }
        }

        public string Name { get; private set; }

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

            return new Playlist(name, songs, currentIndex);
        }
    }
}