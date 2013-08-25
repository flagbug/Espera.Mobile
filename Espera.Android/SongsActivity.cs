using Akavache;
using Android.App;
using Android.OS;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Espera.Android
{
    [Activity(Label = "My Activity")]
    public class SongsActivity : Activity
    {
        private ListView SongsListView
        {
            get { return this.FindViewById<ListView>(Resource.Id.songsList); }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Songs);

            string artist = this.Intent.GetStringExtra("artist");

            IReadOnlyList<Song> songs = BlobCache.InMemory.GetObjectAsync<IReadOnlyList<Song>>("songs").Wait()
                .Where(x => x.Artist.Equals(artist, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var adapter = new SongsAdapter(this, songs);
            this.SongsListView.Adapter = adapter;
            this.SongsListView.ItemClick += (sender, args) =>
                NetworkMessenger.Instance.AddSongToPlaylist(adapter[args.Position]);
        }
    }
}