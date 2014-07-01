using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Songs;
using ReactiveUI;
using Splat;

namespace Espera.Mobile.Core.ViewModels
{
    public class LocalSongsViewModel : ReactiveObject
    {
        private LocalSongViewModel selectedSong;

        public LocalSongsViewModel(IReadOnlyList<LocalSong> songs)
        {
            if (songs == null)
                throw new ArgumentNullException("songs");

            this.Songs = songs.Select(x => new LocalSongViewModel(x)).ToList();

            this.AddToPlaylistCommand = ReactiveCommand.CreateAsyncTask(x => this.QueueSong(this.SelectedSong));

            this.Messages = this.AddToPlaylistCommand.ThrownExceptions
                .Select(_ => "Something went wrong")
                .ObserveOn(RxApp.MainThreadScheduler);
        }

        public ReactiveCommand<Unit> AddToPlaylistCommand { get; private set; }

        public IObservable<string> Messages { get; private set; }

        public LocalSongViewModel SelectedSong
        {
            get { return this.selectedSong; }
            set { this.RaiseAndSetIfChanged(ref this.selectedSong, value); }
        }

        public IReadOnlyList<LocalSongViewModel> Songs { get; private set; }

        private async Task QueueSong(LocalSongViewModel song)
        {
            var file = Locator.Current.GetService<IFile>();
            byte[] data = file.ReadAllBytes(song.Path);

            FileTransferStatus status = await NetworkMessenger.Instance.QueueRemoteSong(song.Model, data);

            song.IsTransfering = true;

            status.TransferProgress.ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    song.TransferProgress = x;
                },
                () => song.IsTransfering = false);
        }
    }
}