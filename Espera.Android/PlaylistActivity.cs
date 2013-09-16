using Android.App;
using Android.OS;
using Android.Widget;
using ReactiveUI;
using ReactiveUI.Android;

namespace Espera.Android
{
    [Activity(Label = "Current Playlist")]
    public class PlaylistActivity : ReactiveActivity<PlaylistViewModel>
    {
        private ListView PlaylistListView
        {
            get { return this.FindViewById<ListView>(Resource.Id.playList); }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Playlist);

            this.ViewModel = new PlaylistViewModel();

            this.OneWayBind(this.ViewModel, x => x.Playlist, x => x.PlaylistListView.Adapter,
                playlist => playlist == null ? null : new PlaylistAdapter(this, playlist));

            this.ViewModel.LoadPlaylistCommand.Execute(null);
        }
    }
}