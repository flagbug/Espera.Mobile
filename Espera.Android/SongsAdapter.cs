using Android.App;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;

namespace Espera.Android
{
    internal class SongsAdapter : BaseAdapter<Song>
    {
        private readonly Activity context;
        private readonly IReadOnlyList<Song> songs;

        public SongsAdapter(Activity context, IReadOnlyList<Song> songs)
        {
            this.context = context;
            this.songs = songs;
        }

        public override int Count
        {
            get { return songs.Count; }
        }

        public override Song this[int position]
        {
            get { return songs[position]; }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView ?? context.LayoutInflater.Inflate(global::Android.Resource.Layout.SimpleListItem2, null);

            view.FindViewById<TextView>(global::Android.Resource.Id.Text1).Text = songs[position].Title;
            view.FindViewById<TextView>(global::Android.Resource.Id.Text2).Text = songs[position].Album;

            return view;
        }
    }
}