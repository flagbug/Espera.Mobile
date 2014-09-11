using System.Collections.Generic;
using Espera.Mobile.Core.Network;
using Espera.Network;
using ReactiveUI;
using System;
using System.Linq;

namespace Espera.Mobile.Core.ViewModels
{
    public class RemoteSongsViewModel : SongsViewModelBase<NetworkSong>
    {
        private readonly ReactiveCommand<ResponseInfo> addToPlaylistCommand;

        public RemoteSongsViewModel(IReadOnlyList<NetworkSong> songs)
        {
            if (songs == null)
                throw new ArgumentNullException("songs");

            this.Songs = songs.Order().ToList();

            this.PlaySongsCommand = ReactiveCommand.CreateAsyncTask(x => NetworkMessenger.Instance.PlaySongsAsync(
                this.Songs.SkipWhile(song => song.Guid != this.SelectedSong.Guid).Select(y => y.Guid).ToList()));

            this.addToPlaylistCommand = ReactiveCommand.CreateAsyncTask(x => NetworkMessenger.Instance.AddSongToPlaylistAsync(this.SelectedSong.Guid));
        }

        public override ReactiveCommand<ResponseInfo> AddToPlaylistCommand
        {
            get { return this.addToPlaylistCommand; }
        }

        public ReactiveCommand<ResponseInfo> PlaySongsCommand { get; private set; }
    }
}