using System;
using Espera.Mobile.Core.SongFetchers;
using Espera.Mobile.Core.Songs;
using Espera.Mobile.Core.ViewModels;
using ReactiveUI;
using Splat;
using Xamarin.Forms;
using System.Reactive.Linq;

namespace Espera.Mobile.Core.UI
{
    public partial class LocalArtistsPage : ContentPage, IViewFor<ArtistsViewModel<LocalSong>>
    {
        public static readonly BindableProperty ViewModelProperty =
            BindableProperty.Create<LocalArtistsPage, ArtistsViewModel<LocalSong>>(x => x.ViewModel, null);

        public LocalArtistsPage()
        {
            InitializeComponent();

            var songFetcher = Locator.CurrentMutable.GetService<ISongFetcher<LocalSong>>();
            this.ViewModel = new ArtistsViewModel<LocalSong>(songFetcher, BlobCacheKeys.SelectedLocalSongs);
            this.BindingContext = this.ViewModel;

            this.ViewModel.Messages.Subscribe(XamFormsApp.Notifications.Notify);

            this.OneWayBind(this.ViewModel, x => x.Artists, x => x.ArtistsListView.ItemsSource);

            this.ViewModel.WhenAnyValue(x => x.Artists).Select(x => x.Count == 0)
                .BindTo(this.EmptyIndicator, x => x.IsVisible);

            this.ArtistsListView.ItemTapped += async (sender, e) => await this.Navigation.PushAsync(new LocalSongsPage());

            this.ViewModel.LoadCommand.Execute(null);
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (ArtistsViewModel<LocalSong>)value; }
        }

        public ArtistsViewModel<LocalSong> ViewModel
        {
            get { return (ArtistsViewModel<LocalSong>)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
    }
}