using Android.Content;
using Android.Views;
using Android.Widget;
using Espera.Android.ViewModels;
using ReactiveUI;
using ReactiveUI.Android;

namespace Espera.Android.Views
{
    public class PlaylistEntryView : ReactiveViewHost<PlaylistEntryViewModel>
    {
        public PlaylistEntryView(Context ctx, ViewGroup parent)
            : base(ctx, Resource.Layout.PlaylistListItem, parent)
        {
            this.OneWayBind(this.ViewModel, vm => vm.Title, v => v.PlaylistEntryTitle);
            this.OneWayBind(this.ViewModel, vm => vm.Artist, v => v.PlaylistEntryArtist);
            this.OneWayBind(this.ViewModel, vm => vm.IsPlaying, v => v.Image.Visibility, x => x ? ViewStates.Visible : ViewStates.Gone);
        }

        public ImageView Image { get; private set; }

        public TextView PlaylistEntryArtist { get; private set; }

        public TextView PlaylistEntryTitle { get; private set; }
    }
}