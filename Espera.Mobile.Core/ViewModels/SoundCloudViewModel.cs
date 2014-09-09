using System;
using System.Collections.Generic;
using Espera.Mobile.Core.Network;
using Espera.Network;
using ReactiveUI;

namespace Espera.Mobile.Core.ViewModels
{
    public class SoundCloudViewModel : SongsViewModelBase<NetworkSong>
    {
        private readonly ReactiveCommand<ResponseInfo> addToPlaylistCommand;
        private string searchTerm;

        public SoundCloudViewModel()
        {
            this.addToPlaylistCommand = ReactiveCommand.CreateAsyncTask(_ => NetworkMessenger.Instance.AddSongToPlaylistAsync(this.SelectedSong.Guid));

            this.LoadCommand = ReactiveCommand.CreateAsyncTask(_ => NetworkMessenger.Instance.GetSoundCloudSongsAsync(this.SearchTerm));

            this.LoadCommand.Subscribe(x => this.Songs = x);
        }

        public override ReactiveCommand<ResponseInfo> AddToPlaylistCommand
        {
            get { return this.addToPlaylistCommand; }
        }

        public ReactiveCommand<IReadOnlyList<NetworkSong>> LoadCommand { get; private set; }

        public string SearchTerm
        {
            get { return this.searchTerm; }
            set { this.RaiseAndSetIfChanged(ref this.searchTerm, value); }
        }
    }
}