using Android.App;
using Android.Views;
using Android.Widget;
using Espera.Network;
using ReactiveUI;
using ReactiveUI.Android;

namespace Espera.Android.Views
{
    internal class RemoteSongsAdapter : ReactiveListAdapter<NetworkSong>
    {
        public RemoteSongsAdapter(Activity context, IReadOnlyReactiveList<NetworkSong> songs)
            : base(songs, (song, parent) => CreateView(context), MapModel)
        { }

        private static View CreateView(Activity ctx)
        {
            return ctx.LayoutInflater.Inflate(global::Android.Resource.Layout.SimpleListItem2, null);
        }

        private static void MapModel(NetworkSong song, View view)
        {
            view.FindViewById<TextView>(global::Android.Resource.Id.Text1).Text = song.Title;
            view.FindViewById<TextView>(global::Android.Resource.Id.Text2).Text = song.Album;
        }
    }
}