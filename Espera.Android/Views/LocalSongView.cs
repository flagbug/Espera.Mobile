using Android.Content;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core.ViewModels;
using ReactiveUI;

namespace Espera.Android.Views
{
    public class LocalSongView : ReactiveViewHost<LocalSongViewModel>
    {
        public LocalSongView(Context ctx, LocalSongViewModel viewModel, ViewGroup parent)
            : base(ctx, Resource.Layout.LocalSongItem, parent)
        {
            this.ViewModel = viewModel;
            this.OneWayBind(this.ViewModel, vm => vm.Title, v => v.LocalSongTitle.Text);
            this.OneWayBind(this.ViewModel, vm => vm.Album, v => v.LocalSongAlbum.Text);
            this.OneWayBind(this.ViewModel, vm => vm.TransferProgress, v => v.LocalSongTransferProgress.Progress);
            this.OneWayBind(this.ViewModel, vm => vm.IsTransfering, v => v.LocalSongTransferProgress.Visibility,
                x => x ? ViewStates.Visible : ViewStates.Gone);
        }

        public TextView LocalSongAlbum { get; private set; }

        public TextView LocalSongTitle { get; private set; }

        public ProgressBar LocalSongTransferProgress { get; private set; }
    }
}