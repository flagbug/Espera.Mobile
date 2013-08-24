using Akavache;
using Android.App;
using Android.OS;
using Android.Widget;
using System.Collections;
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

            IReadOnlyList<Song> songs = BlobCache.LocalMachine.GetObjectAsync<IReadOnlyList<Song>>("songs").Wait()
                .Where(x => x.Artist == artist)
                .ToList();

            this.SongsListView.Adapter = new ArrayAdapter(this, global::Android.Resource.Layout.SimpleListItem1, (IList)songs);
        }
    }
}