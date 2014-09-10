using System;
using System.Reactive.Linq;
using Android.Content;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core.ViewModels;
using ReactiveUI;
using Splat;

namespace Espera.Android.Views
{
    public class SoundCloudSongView : ReactiveViewHost<SoundCloudSongViewModel>
    {
        public SoundCloudSongView(Context ctx, SoundCloudSongViewModel viewModel, ViewGroup parent)
            : base(ctx, Resource.Layout.SoundCloudSongItem, parent)
        {
            this.Artwork.SetImageDrawable(null);

            this.ViewModel = viewModel;

            this.OneWayBind(this.ViewModel, vm => vm.Title, v => v.SoundCloudSongTitle.Text);
            this.OneWayBind(this.ViewModel, vm => vm.Artist, v => v.SoundCloudSongArtist.Text);

            this.Artwork.Visibility = ViewStates.Invisible;
            this.WhenAnyValue(x => x.ViewModel.Artwork).Where(x => x != null)
                .Select(x => x.ToNative()).Subscribe(x =>
                {
                    this.Artwork.SetImageDrawable(x);
                    this.Artwork.Visibility = ViewStates.Visible;
                });
        }

        public ImageView Artwork { get; private set; }

        public TextView SoundCloudSongArtist { get; private set; }

        public TextView SoundCloudSongTitle { get; private set; }
    }
}