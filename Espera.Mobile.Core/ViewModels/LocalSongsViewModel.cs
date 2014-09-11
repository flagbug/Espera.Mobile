using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Espera.Mobile.Core.Network;
using Espera.Network;
using ReactiveMarrow;
using ReactiveUI;
using Splat;

namespace Espera.Mobile.Core.ViewModels
{
    public class LocalSongsViewModel : SongsViewModelBase<LocalSongViewModel>
    {
        private readonly ReactiveCommand<ResponseInfo> addToPlaylistCommand;

        public LocalSongsViewModel(IReadOnlyList<LocalSong> songs)
        {
            if (songs == null)
                throw new ArgumentNullException("songs");

            this.Songs = songs.Order().Select(x => new LocalSongViewModel(x)).ToList();

            this.addToPlaylistCommand = ReactiveCommand.CreateAsyncTask(x => this.QueueSong(this.SelectedSong));
        }

        public override ReactiveCommand<ResponseInfo> AddToPlaylistCommand
        {
            get { return this.addToPlaylistCommand; }
        }

        private async Task<ResponseInfo> QueueSong(LocalSongViewModel song)
        {
            var file = Locator.Current.GetService<IFile>();
            byte[] data = file.ReadAllBytes(song.Path);

            FileTransferStatus status = await NetworkMessenger.Instance.QueueRemoteSong(song.Model, data);

            if (status.ResponseInfo.Status == ResponseStatus.Success)
            {
                song.IsTransfering = true;

                status.TransferProgress.ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(x =>
                    {
                        song.TransferProgress = x;
                    },
                    () => song.IsTransfering = false);
            }

            return status.ResponseInfo;
        }
    }
}