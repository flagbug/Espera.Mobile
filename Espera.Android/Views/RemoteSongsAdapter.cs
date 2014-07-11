using Android.App;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core.Songs;
using ReactiveUI;

namespace Espera.Android.Views
{
    internal class RemoteSongsAdapter : ReactiveListAdapter<RemoteSong>
    {
        public RemoteSongsAdapter(Activity context, IReadOnlyReactiveList<RemoteSong> songs)
            : base(songs, (song, parent) => CreateView(context), MapModel)
        { }

        private static View CreateView(Activity ctx)
        {
            return ctx.LayoutInflater.Inflate(global::Android.Resource.Layout.SimpleListItem2, null);
        }

        private static void MapModel(RemoteSong song, View view)
        {
            view.FindViewById<TextView>(global::Android.Resource.Id.Text1).Text = song.Title;
            view.FindViewById<TextView>(global::Android.Resource.Id.Text2).Text = song.Album;
        }
    }
}