using Android.App;
using Android.Views;
using Android.Widget;
using Espera.Network;
using ReactiveUI;

namespace Espera.Android.Views
{
    internal class SoundCloudSongsAdapter : ReactiveListAdapter<NetworkSong>
    {
        public SoundCloudSongsAdapter(Activity context, IReadOnlyReactiveList<NetworkSong> songs)
            : base(songs, (song, parent) => CreateView(context), MapModel)
        { }

        private static View CreateView(Activity ctx)
        {
            return ctx.LayoutInflater.Inflate(Resource.Layout.SoundCloudSongItem, null);
        }

        private static void MapModel(NetworkSong song, View view)
        {
            view.FindViewById<TextView>(Resource.Id.SoundCloudSongTitle).Text = song.Title;
            view.FindViewById<TextView>(Resource.Id.SoundCloudSongArtist).Text = song.Artist;
        }
    }
}