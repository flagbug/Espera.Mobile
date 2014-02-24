using Android.App;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core;
using ReactiveUI;
using ReactiveUI.Android;

namespace Espera.Android.Views
{
    internal class SongsAdapter : ReactiveListAdapter<Song>
    {
        public SongsAdapter(Activity context, IReadOnlyReactiveList<Song> songs)
            : base(songs, (song, parent) => CreateView(context), MapModel)
        { }

        private static View CreateView(Activity ctx)
        {
            return ctx.LayoutInflater.Inflate(global::Android.Resource.Layout.SimpleListItem2, null);
        }

        private static void MapModel(Song song, View view)
        {
            view.FindViewById<TextView>(global::Android.Resource.Id.Text1).Text = song.Title;
            view.FindViewById<TextView>(global::Android.Resource.Id.Text2).Text = song.Album;
        }
    }
}