using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Akavache;
using Espera.Mobile.Core.Songs;
using Espera.Mobile.Core.ViewModels;
using ReactiveUI;
using Xamarin.Forms;

namespace Espera.Mobile.Core.UI
{
    public partial class LocalSongsPage : ContentPage, IViewFor<LocalSongsViewModel>
    {
        public static readonly BindableProperty ViewModelProperty =
            BindableProperty.Create<LocalSongsPage, LocalSongsViewModel>(x => x.ViewModel, null);

        public LocalSongsPage()
        {
            InitializeComponent();

            var songs = BlobCache.InMemory.GetObjectAsync<IEnumerable<LocalSong>>(BlobCacheKeys.SelectedLocalSongs).Wait().ToList();

            this.ViewModel = new LocalSongsViewModel(songs);
            this.BindingContext = this.ViewModel;

            this.ViewModel.Messages.Subscribe(XamFormsApp.Notifications.Notify);

            this.SongsListView.Events().ItemTapped.Subscribe(async _ => await this.ViewModel.AddToPlaylistCommand.ExecuteAsync());
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (LocalSongsViewModel)value; }
        }

        public LocalSongsViewModel ViewModel
        {
            get { return (LocalSongsViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
    }
}