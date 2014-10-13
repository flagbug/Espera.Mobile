using Android.Content;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core;
using Espera.Mobile.Core.ViewModels;
using ReactiveUI;

namespace Espera.Android.Views
{
    public class RemoteSongView : ReactiveViewHost<RemoteSongViewModel>
    {
        public RemoteSongView(Context ctx, RemoteSongViewModel viewModel, ViewGroup parent)
            : base(ctx, Resource.Layout.RemoteSongItem, parent)
        {
            this.ViewModel = viewModel;

            this.OneWayBind(this.ViewModel, vm => vm.Title, v => v.RemoteSongTitle.Text);
            this.OneWayBind(this.ViewModel, vm => vm.Artist, v => v.RemoteSongArtist.Text);
            this.OneWayBind(this.ViewModel, vm => vm.Duration, v => v.RemoteSongDuration.Text, x => x.FormatAdaptive());
        }

        public TextView RemoteSongArtist { get; private set; }

        public TextView RemoteSongDuration { get; private set; }

        public TextView RemoteSongTitle { get; private set; }
    }
}