using System;
using System.Reactive.Linq;
using Android.Content;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core;
using Espera.Mobile.Core.ViewModels;
using Humanizer;
using ReactiveUI;
using Splat;

namespace Espera.Android.Views
{
    public class YoutubeSongView : ReactiveViewHost<YoutubeSongViewModel>
    {
        public YoutubeSongView(Context ctx, YoutubeSongViewModel viewModel, ViewGroup parent)
            : base(ctx, Resource.Layout.YoutubeSongItem, parent)
        {
            this.ViewModel = viewModel;

            this.WhenAnyValue(x => x.ViewModel)
                .Subscribe(x => this.Artwork.SetImageDrawable(null));

            this.OneWayBind(this.ViewModel, vm => vm.Title, v => v.YoutubeSongTitle.Text);
            this.OneWayBind(this.ViewModel, vm => vm.Uploader, v => v.YoutubeSongUploader.Text);
            this.OneWayBind(this.ViewModel, vm => vm.Views, v => v.YoutubeSongViews.Text,
                x => ctx.Resources.GetString(Resource.String.youtube_view).ToQuantity(x, "N0"));
            this.OneWayBind(this.ViewModel, vm => vm.Duration, v => v.YoutubeSongDuration.Text, x => x.FormatAdaptive());

            this.WhenAnyValue(x => x.ViewModel.Artwork)
                .Where(x => x != null)
                .Select(x => x.ToNative())
                .Subscribe(x => this.Artwork.SetImageDrawable(x));
        }

        public ImageView Artwork { get; private set; }

        public TextView YoutubeSongDuration { get; private set; }

        public TextView YoutubeSongTitle { get; private set; }

        public TextView YoutubeSongUploader { get; private set; }

        public TextView YoutubeSongViews { get; private set; }
    }
}