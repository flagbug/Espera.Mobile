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
    public class YoutubeSongView : ReactiveViewHost<YoutubeSongViewModel>
    {
        public YoutubeSongView(Context ctx, YoutubeSongViewModel viewModel, ViewGroup parent)
            : base(ctx, Resource.Layout.YoutubeSongItem, parent)
        {
            this.ViewModel = viewModel;

            this.OneWayBind(this.ViewModel, vm => vm.Title, v => v.YoutubeSongTitle.Text);

            this.Artwork.SetImageDrawable(null);
            this.Artwork.Visibility = ViewStates.Invisible;

            this.WhenAnyValue(x => x.ViewModel.Artwork)
                .Where(x => x != null)
                .Select(x => x.ToNative())
                .Subscribe(x =>
                {
                    this.Artwork.SetImageDrawable(x);
                    this.Artwork.Visibility = ViewStates.Visible;
                });
        }

        public ImageView Artwork { get; private set; }

        public TextView YoutubeSongTitle { get; private set; }
    }
}