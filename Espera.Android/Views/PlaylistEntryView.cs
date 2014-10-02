using Android.Content;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core;
using Espera.Mobile.Core.ViewModels;
using ReactiveUI;

namespace Espera.Android.Views
{
    public class PlaylistEntryView : ReactiveViewHost<PlaylistEntryViewModel>
    {
        public PlaylistEntryView(Context ctx, PlaylistEntryViewModel viewModel, ViewGroup parent)
            : base(ctx, Resource.Layout.PlaylistListItem, parent)
        {
            this.ViewModel = viewModel;
            this.OneWayBind(this.ViewModel, vm => vm.Title, v => v.PlaylistEntryTitle.Text);
            this.OneWayBind(this.ViewModel, vm => vm.Artist, v => v.PlaylistEntryArtist.Text);
            this.OneWayBind(this.ViewModel, vm => vm.IsPlaying, v => v.Image.Visibility, x => x ? ViewStates.Visible : ViewStates.Gone);
            this.OneWayBind(this.ViewModel, vm => vm.Duration, v => v.PlaylistEntryDuration.Text, x => x.FormatAdaptive());
        }

        public ImageView Image { get; private set; }

        public TextView PlaylistEntryArtist { get; private set; }

        public TextView PlaylistEntryDuration { get; private set; }

        public TextView PlaylistEntryTitle { get; private set; }
    }
}