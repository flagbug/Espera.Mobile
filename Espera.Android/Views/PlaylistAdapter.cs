using System;
using System.Reactive.Linq;
using Android.App;
using Android.Views;
using Android.Widget;
using ReactiveUI;

namespace Espera.Android.Views
{
    internal class PlaylistAdapter : BaseAdapter<Song>
    {
        private readonly IDisposable changedSubscription;
        private readonly Activity context;
        private readonly Playlist playlist;

        public PlaylistAdapter(Activity context, Playlist playlist)
        {
            this.context = context;
            this.playlist = playlist;

            this.changedSubscription = this.playlist.Changed
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => this.NotifyDataSetChanged());
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
            View view = convertView ?? context.LayoutInflater.Inflate(Resource.Layout.PlaylistListItem, null);

            Song song = this.playlist.Songs[position];
            view.FindViewById<TextView>(Resource.Id.PlaylistItemText1).Text = song.Title;
            view.FindViewById<TextView>(Resource.Id.PlaylistItemText2).Text = song.Source == SongSource.Local ? song.Artist : "YouTube";
            view.FindViewById<ImageView>(Resource.Id.Image).Visibility = position == playlist.CurrentIndex ? ViewStates.Visible : ViewStates.Gone;

            return view;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.changedSubscription.Dispose();
        }
    }
}