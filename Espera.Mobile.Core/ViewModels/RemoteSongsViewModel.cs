using System.Collections.Generic;
using Espera.Mobile.Core.Network;
using Espera.Network;
using ReactiveUI;
using System;
using System.Linq;

namespace Espera.Mobile.Core.ViewModels
{
    public class RemoteSongsViewModel : SongsViewModelBase<RemoteSongViewModel>
    {
        private readonly ReactiveCommand<ResponseInfo> addToPlaylistCommand;

        public RemoteSongsViewModel(IReadOnlyList<NetworkSong> songs)
        {
            if (songs == null)
                throw new ArgumentNullException("songs");

            this.Songs = songs.Order().Select(x => new RemoteSongViewModel(x)).ToList();

            this.PlaySongsCommand = ReactiveCommand.CreateAsyncTask(x => NetworkMessenger.Instance.PlaySongsAsync(
                this.Songs.SkipWhile(song => song.Model.Guid != this.SelectedSong.Model.Guid).Select(y => y.Model.Guid).ToList()));

            this.addToPlaylistCommand = ReactiveCommand.CreateAsyncTask(x => NetworkMessenger.Instance.AddSongToPlaylistAsync(this.SelectedSong.Model.Guid));
        }

        public override ReactiveCommand<ResponseInfo> AddToPlaylistCommand
        {
            get { return this.addToPlaylistCommand; }
        }

        public ReactiveCommand<ResponseInfo> PlaySongsCommand { get; private set; }
    }
}