using Android.App;
using Android.Views;
using Android.Widget;

namespace Espera.Android
{
    internal class PlaylistAdapter : BaseAdapter<Song>
    {
        private readonly Activity context;
        private readonly Playlist playlist;

        public PlaylistAdapter(Activity context, Playlist playlist)
        {
            this.context = context;
            this.playlist = playlist;
        }

        public override int Count
        {
            get { return this.playlist.Songs.Count; }
        }

        public override Song this[int position]
        {
            get { return this.playlist.Songs[position]; }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView ?? context.LayoutInflater.Inflate(global::Android.Resource.Layout.SimpleListItem1, null);

            view.FindViewById<TextView>(global::Android.Resource.Id.Text1).Text = this.playlist.Songs[position].Title;

            return view;
        }
    }
}