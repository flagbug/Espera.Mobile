using System.Collections.Generic;
using System.Reactive.Disposables;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Songs;
using Espera.Network;
using ReactiveMarrow;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace Espera.Mobile.Core.ViewModels
{
    public class RemoteSongsViewModel : ReactiveObject, ISupportsActivation
    {
        private ObservableAsPropertyHelper<bool> isAdmin;
        private ObservableAsPropertyHelper<int?> remainingVotes;
        private RemoteSong selectedSong;

        public RemoteSongsViewModel(IReadOnlyList<RemoteSong> songs)
        {
            if (songs == null)
                throw new ArgumentNullException("songs");

            this.Songs = songs.Order().ToList();

            this.Activator = new ViewModelActivator();

            this.PlaySongsCommand = ReactiveCommand.CreateAsyncTask(x => NetworkMessenger.Instance.PlaySongsAsync(
                this.Songs.SkipWhile(song => song.Guid != this.SelectedSong.Guid).Select(y => y.Guid).ToList()));
            var playSongsMessage = this.PlaySongsCommand
                .Select(x => x.Status == ResponseStatus.Success ? "Playing songs" : "Error adding songs");

            this.AddToPlaylistCommand = ReactiveCommand.CreateAsyncTask(x => NetworkMessenger.Instance.AddSongToPlaylistAsync(this.SelectedSong.Guid));
            var addToPlaylistMessage = this.AddToPlaylistCommand
                .Select(x => x.Status == ResponseStatus.Success ? "Song added to playlist" : "Error adding song");

            this.Messages = playSongsMessage.Merge(addToPlaylistMessage)
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

        public ReactiveCommand<ResponseInfo> AddToPlaylistCommand { get; private set; }

        public bool IsAdmin
        {
            get { return this.isAdmin.Value; }
        }

        public IObservable<string> Messages { get; private set; }

        public ReactiveCommand<ResponseInfo> PlaySongsCommand { get; private set; }

        public int? RemainingVotes
        {
            get { return this.remainingVotes.Value; }
        }

        public RemoteSong SelectedSong
        {
            get { return this.selectedSong; }
            set { this.RaiseAndSetIfChanged(ref this.selectedSong, value); }
        }

        public IReadOnlyList<RemoteSong> Songs { get; private set; }
    }
}