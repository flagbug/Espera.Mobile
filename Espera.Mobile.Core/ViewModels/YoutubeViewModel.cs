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
    public class YoutubeViewModel : SongsViewModelBase<YoutubeSongViewModel>
    {
        private readonly ReactiveCommand<ResponseInfo> addToPlaylistCommand;
        private string searchTerm;

        public YoutubeViewModel()
        {
            this.addToPlaylistCommand = ReactiveCommand.CreateAsyncTask(_ => NetworkMessenger.Instance.AddSongToPlaylistAsync(this.SelectedSong.Guid));

            this.LoadCommand = ReactiveCommand.CreateAsyncTask(_ => this.LoadSongs());

            this.LoadCommand.Subscribe(x => this.Songs = x);
        }

        public override ReactiveCommand<ResponseInfo> AddToPlaylistCommand
        {
            get { return this.addToPlaylistCommand; }
        }

        public ReactiveCommand<IReadOnlyList<YoutubeSongViewModel>> LoadCommand { get; private set; }

        public string SearchTerm
        {
            get { return this.searchTerm; }
            set { this.RaiseAndSetIfChanged(ref this.searchTerm, value); }
        }

        private async Task<IReadOnlyList<YoutubeSongViewModel>> LoadSongs()
        {
            var networkSongs = await BlobCache.LocalMachine.GetOrFetchObject("youtube-" + this.SearchTerm,
                () => NetworkMessenger.Instance.GetSoundCloudSongsAsync(this.SearchTerm), DateTimeOffset.Now + TimeSpan.FromMinutes(15));

            return (IReadOnlyList<YoutubeSongViewModel>)networkSongs.Select(x => new YoutubeSongViewModel(x)).ToList();
        }
    }
}