using Android.App;
using Android.Views;
using Android.Widget;
using Espera.Android.ViewModels;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace Espera.Android.Views
{
    internal class PlaylistAdapter : BaseAdapter<PlaylistEntryViewModel>
    {
        private readonly IDisposable changedSubscription;
        private readonly Activity context;
        private readonly IReadOnlyReactiveList<PlaylistEntryViewModel> playlist;

        public PlaylistAdapter(Activity context, IReadOnlyReactiveList<PlaylistEntryViewModel> playlist)
        {
            this.context = context;
            this.playlist = playlist;

            this.changedSubscription = this.playlist.Changed
                .Buffer(TimeSpan.FromMilliseconds(100), RxApp.MainThreadScheduler)
                .Where(x => x.Any())
                .Subscribe(_ => this.NotifyDataSetChanged());
        }

        public override int Count
        {
            get { return this.playlist.Count; }
        }

        public override PlaylistEntryViewModel this[int position]
        {
            get { return this.playlist[position]; }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView ?? context.LayoutInflater.Inflate(Resource.Layout.PlaylistListItem, null);

            PlaylistEntryViewModel entry = this.playlist[position];
            view.FindViewById<TextView>(Resource.Id.PlaylistItemText1).Text = entry.Title;
            view.FindViewById<TextView>(Resource.Id.PlaylistItemText2).Text = entry.Artist;
            view.FindViewById<ImageView>(Resource.Id.Image).Visibility = entry.IsPlaying ? ViewStates.Visible : ViewStates.Gone;

            return view;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.changedSubscription.Dispose();
        }
    }
}