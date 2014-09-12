using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using Espera.Mobile.Core.Network;
using Espera.Network;
using ReactiveUI;

namespace Espera.Mobile.Core.ViewModels
{
    public class SoundCloudViewModel : SongsViewModelBase<SoundCloudSongViewModel>
    {
        private readonly ReactiveCommand<ResponseInfo> addToPlaylistCommand;
        private string searchTerm;

        public SoundCloudViewModel()
        {
            this.addToPlaylistCommand = ReactiveCommand.CreateAsyncTask(_ => NetworkMessenger.Instance.AddSongToPlaylistAsync(this.SelectedSong.Guid));

            this.LoadCommand = ReactiveCommand.CreateAsyncTask(_ => this.LoadSongs());

            this.LoadCommand.Subscribe(x => this.Songs = x);
        }

        public override ReactiveCommand<ResponseInfo> AddToPlaylistCommand
        {
            get { return this.addToPlaylistCommand; }
        }

        public ReactiveCommand<IReadOnlyList<SoundCloudSongViewModel>> LoadCommand { get; private set; }

        public string SearchTerm
        {
            get { return this.searchTerm; }
            set { this.RaiseAndSetIfChanged(ref this.searchTerm, value); }
        }

        private async Task<IReadOnlyList<SoundCloudSongViewModel>> LoadSongs()
        {
            var networkSongs = await BlobCache.LocalMachine.GetOrFetchObject("soundcloud-" + this.SearchTerm,
                () => NetworkMessenger.Instance.GetSoundCloudSongsAsync(this.SearchTerm), DateTimeOffset.Now + TimeSpan.FromMinutes(15));

            return (IReadOnlyList<SoundCloudSongViewModel>)networkSongs.Select(x => new SoundCloudSongViewModel(x)).ToList();
        }
    }
}