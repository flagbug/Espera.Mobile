using Espera.Mobile.Core.Network;
using Espera.Network;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Threading.Tasks;

namespace Espera.Mobile.Core.ViewModels
{
    public class RemoteSongsViewModel : SongsViewModelBase<RemoteSongViewModel>
    {
        private ReactiveCommand<Unit> addToPlaylistCommand;

        public RemoteSongsViewModel(IReadOnlyList<NetworkSong> songs)
        {
            if (songs == null)
                throw new ArgumentNullException(nameof(songs));

            this.Songs = songs.Order().Select(x => new RemoteSongViewModel(x)).ToList();

            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                this.PlaySongsCommand = ReactiveCommand.CreateAsyncObservable(x => NetworkMessenger.Instance.PlaySongsAsync(
                    this.GetSongGuidsToPlay()).ToObservable().TakeUntil(disposable));

                this.addToPlaylistCommand = ReactiveCommand.CreateAsyncObservable(x =>
                    NetworkMessenger.Instance.AddSongToPlaylistAsync(this.SelectedSong.Model.Guid).ToObservable().TakeUntil(disposable));

                return disposable;
            });
        }

        public override ReactiveCommand<Unit> AddToPlaylistCommand
        {
            get { return this.addToPlaylistCommand; }
        }

        public ReactiveCommand<Unit> PlaySongsCommand { get; private set; }

        private IReadOnlyList<Guid> GetSongGuidsToPlay() => this.Songs.SkipWhile(song => song.Model.Guid != this.SelectedSong.Model.Guid).Select(y => y.Model.Guid).ToList();
    }
}