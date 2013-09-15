using Akavache;
using Android.App;
using Android.OS;
using Android.Widget;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Espera.Android
{
    [Activity(Label = "Songs")]
    public class SongsActivity : Activity
    {
        private ListView SongsListView
        {
            get { return this.FindViewById<ListView>(Resource.Id.songsList); }
        }

        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Songs);

            string artist = this.Intent.GetStringExtra("artist");

            IReadOnlyList<Song> songs = await BlobCache.InMemory.GetObjectAsync<IReadOnlyList<Song>>("songs");

            songs = songs.Where(x => x.Artist.Equals(artist, StringComparison.OrdinalIgnoreCase))
               .ToList();

            var adapter = new SongsAdapter(this, songs);
            this.SongsListView.Adapter = adapter;
            this.SongsListView.ItemClick += async (sender, args) =>
            {
                var guids = new List<Guid>();

                for (int i = args.Position; i < adapter.Count; i++)
                {
                    guids.Add(adapter[i].Guid);
                }

                Tuple<int, string> response = await NetworkMessenger.Instance.PlaySongs(guids);

                string text = response.Item1 == 200 ? "Playing songs" : "Error adding songs";

                Toast.MakeText(this, text, ToastLength.Short).Show();

                /*
                Tuple<int, string> response = await NetworkMessenger.Instance.AddSongToPlaylist(adapter[args.Position]);

                string text = response.Item1 == 200 ? "Song added to playlist" : "Error adding song";

                Toast.MakeText(this, text, ToastLength.Short).Show();*/
            };
        }
    }
}