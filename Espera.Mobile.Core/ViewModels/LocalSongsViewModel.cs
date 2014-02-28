using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Songs;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Espera.Mobile.Core.ViewModels
{
    public class LocalSongsViewModel : ReactiveObject
    {
        public LocalSongsViewModel(IReadOnlyReactiveList<LocalSong> songs)
        {
            if (songs == null)
                throw new ArgumentNullException("songs");

            this.Songs = songs.CreateDerivedCollection(x => new LocalSongViewModel(x));

            this.AddToPlaylistCommand = new ReactiveCommand();
            this.AddToPlaylistCommand.RegisterAsyncTask(x =>
                this.QueueSong(this.Songs[(int)x]))
                .Subscribe();

            this.Message = Observable.Never<string>()
                .Throttle(TimeSpan.FromMilliseconds(200))
                .ObserveOn(RxApp.MainThreadScheduler);
        }

        public ReactiveCommand AddToPlaylistCommand { get; private set; }

        public IObservable<string> Message { get; private set; }

        public IReadOnlyReactiveList<LocalSongViewModel> Songs { get; private set; }

        private async Task QueueSong(LocalSongViewModel song)
        {
            byte[] data = File.ReadAllBytes(song.Path);

            FileTransferStatus status = await NetworkMessenger.Instance.QueueRemoteSong(data);

            data = null;

            song.IsTransfering = true;

            status.TransferProgress.Subscribe(x =>
            {
                song.TransferProgress = x;
            },
            () => song.IsTransfering = false);
        }
    }
}