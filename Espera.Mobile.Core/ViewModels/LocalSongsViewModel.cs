using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Songs;
using Espera.Network;
using ReactiveMarrow;
using ReactiveUI;
using Splat;

namespace Espera.Mobile.Core.ViewModels
{
    public class LocalSongsViewModel : ReactiveObject, ISupportsActivation
    {
        private ObservableAsPropertyHelper<bool> isAdmin;
        private ObservableAsPropertyHelper<int?> remainingVotes;
        private LocalSongViewModel selectedSong;

        public LocalSongsViewModel(IReadOnlyList<LocalSong> songs)
        {
            if (songs == null)
                throw new ArgumentNullException("songs");

            this.Songs = songs.Order().Select(x => new LocalSongViewModel(x)).ToList();

            this.Activator = new ViewModelActivator();

            this.AddToPlaylistCommand = ReactiveCommand.CreateAsyncTask(x => this.QueueSong(this.SelectedSong));

            this.Messages = this.AddToPlaylistCommand.ThrownExceptions
                .Select(_ => "Something went wrong")
                .ObserveOn(RxApp.MainThreadScheduler);

            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                this.remainingVotes = NetworkMessenger.Instance.WhenAnyValue(x => x.GuestSystemInfo)
                    .Select(x => x.IsEnabled ? new int?(x.RemainingVotes) : null)
                    .ToProperty(this, x => x.RemainingVotes)
                    .DisposeWith(disposable);

                this.isAdmin = NetworkMessenger.Instance.WhenAnyValue(x => x.AccessPermission, x => x == NetworkAccessPermission.Admin)
                    .ToProperty(this, x => x.IsAdmin)
                    .DisposeWith(disposable);

                return disposable;
            });
        }

        public ViewModelActivator Activator { get; private set; }

        public ReactiveCommand<Unit> AddToPlaylistCommand { get; private set; }

        public bool IsAdmin
        {
            get { return this.isAdmin.Value; }
        }

        public IObservable<string> Messages { get; private set; }

        public int? RemainingVotes
        {
            get { return this.remainingVotes.Value; }
        }

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