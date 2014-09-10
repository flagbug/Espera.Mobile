using System;
using System.Collections.Generic;
using System.Linq;
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

            this.LoadCommand = ReactiveCommand.CreateAsyncTask(async _ =>
            {
                var networkSongs = await NetworkMessenger.Instance.GetSoundCloudSongsAsync(this.SearchTerm);

                return (IReadOnlyList<SoundCloudSongViewModel>)networkSongs.Select(x => new SoundCloudSongViewModel(x)).ToList();
            });

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
    }
}