using System;
using System.Reactive.Linq;
using Android.Content;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core.ViewModels;
using Humanizer;
using ReactiveUI;
using Splat;

namespace Espera.Android.Views
{
    public class SoundCloudSongView : ReactiveViewHost<SoundCloudSongViewModel>
    {
        public SoundCloudSongView(Context ctx, SoundCloudSongViewModel viewModel, ViewGroup parent)
            : base(ctx, Resource.Layout.SoundCloudSongItem, parent)
        {
            this.ViewModel = viewModel;

            this.WhenAnyValue(x => x.ViewModel)
                .Subscribe(x => this.Artwork.SetImageDrawable(null));

            this.OneWayBind(this.ViewModel, vm => vm.Title, v => v.SoundCloudSongTitle.Text);
            this.OneWayBind(this.ViewModel, vm => vm.Artist, v => v.SoundCloudSongArtist.Text);
            this.OneWayBind(this.ViewModel, vm => vm.PlaybackCount, v => v.SoundCloudSongPlaybackCount.Text,
                x => ctx.Resources.GetString(Resource.String.play).ToQuantity(x, "N0"));

            this.WhenAnyValue(x => x.ViewModel.Artwork)
                .Where(x => x != null)
                .Select(x => x.ToNative())
                .Subscribe(x => this.Artwork.SetImageDrawable(x));
        }

        public ImageView Artwork { get; private set; }

        public TextView SoundCloudSongArtist { get; private set; }

        public TextView SoundCloudSongPlaybackCount { get; private set; }

        public TextView SoundCloudSongTitle { get; private set; }
    }
}