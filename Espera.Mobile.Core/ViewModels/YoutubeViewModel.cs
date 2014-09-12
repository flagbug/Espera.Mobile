using System;
using System.Collections.Generic;
using System.Linq;
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

            this.LoadCommand = ReactiveCommand.CreateAsyncTask(async _ =>
            {
                var networkSongs = await NetworkMessenger.Instance.GetYoutubeSongsAsync(this.SearchTerm);

                return (IReadOnlyList<YoutubeSongViewModel>)networkSongs.Select(x => new YoutubeSongViewModel(x)).ToList();
            });

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
    }
}