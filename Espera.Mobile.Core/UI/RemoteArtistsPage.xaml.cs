using System;
using System.Reactive.Linq;
using Espera.Mobile.Core.SongFetchers;
using Espera.Mobile.Core.Songs;
using Espera.Mobile.Core.ViewModels;
using ReactiveUI;
using Xamarin.Forms;

namespace Espera.Mobile.Core.UI
{
    public partial class RemoteArtistsPage : ContentPage, IViewFor<ArtistsViewModel<RemoteSong>>
    {
        public static readonly BindableProperty ViewModelProperty =
            BindableProperty.Create<RemoteArtistsPage, ArtistsViewModel<RemoteSong>>(x => x.ViewModel, null);

        public RemoteArtistsPage()
        {
            InitializeComponent();

            this.ViewModel = new ArtistsViewModel<RemoteSong>(new RemoteSongFetcher());
            this.BindingContext = this.ViewModel;

            this.ViewModel.LoadCommand.IsExecuting
                .BindTo(this.LoadIndicator, x => x.IsVisible);
            this.ViewModel.LoadCommand.IsExecuting
                .BindTo(this.LoadIndicator, x => x.IsRunning);

            this.ViewModel.LoadCommand.IsExecuting
                .CombineLatest(this.ViewModel.WhenAnyValue(x => x.Artists, x => x.Count == 0), (loading, empty) => !loading && empty)
                .BindTo(this.EmptyIndicator, x => x.IsVisible);

            this.ViewModel.Messages.Subscribe(async x =>
            {
                await this.DisplayAlert("Error", x, "Ok", null);
                //await Navigation.PopAsync();
            });

            this.ArtistsListView.ItemTapped += async (sender, e) => await this.Navigation.PushAsync(new RemoteSongsPage());
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (ArtistsViewModel<RemoteSong>)value; }
        }

        public ArtistsViewModel<RemoteSong> ViewModel
        {
            get { return (ArtistsViewModel<RemoteSong>)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            this.ViewModel.LoadCommand.Execute(null);
        }
    }
}