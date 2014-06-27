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
    public partial class RemoteSongsPage : ContentPage, IViewFor<RemoteSongsViewModel>
    {
        public static readonly BindableProperty ViewModelProperty =
            BindableProperty.Create<RemoteSongsPage, RemoteSongsViewModel>(x => x.ViewModel, null);

        public RemoteSongsPage()
        {
            InitializeComponent();

            var songs = BlobCache.InMemory.GetObjectAsync<IEnumerable<RemoteSong>>(BlobCacheKeys.SelectedRemoteSongs).Wait();

            this.Title = songs.First().Artist;

            this.ViewModel = new RemoteSongsViewModel(songs.ToList());
            this.BindingContext = this.ViewModel;

            this.SongsListView.ItemTapped += async (sender, e) =>
            {
                var actions = new[] { "Play", "Add To Playlist" };

                string result = await this.DisplayActionSheet("Playback Actions", "Cancel", null, actions);

                if (result == actions[0])
                {
                    await this.ViewModel.PlaySongsCommand.ExecuteAsync();
                }

                else if (result == actions[1])
                {
                    await this.ViewModel.AddToPlaylistCommand.ExecuteAsync();
                }
            };
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (RemoteSongsViewModel)value; }
        }

        public RemoteSongsViewModel ViewModel
        {
            get { return (RemoteSongsViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
    }
}