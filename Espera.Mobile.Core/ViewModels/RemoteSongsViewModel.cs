using System.Collections.Generic;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Songs;
using Espera.Network;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace Espera.Mobile.Core.ViewModels
{
    public class RemoteSongsViewModel : ReactiveObject
    {
        private RemoteSong selectedSong;

        public RemoteSongsViewModel(IReadOnlyList<RemoteSong> songs)
        {
            if (songs == null)
                throw new ArgumentNullException("songs");

            this.Songs = songs;

            this.PlaySongsCommand = ReactiveCommand.CreateAsyncTask(x => NetworkMessenger.Instance.PlaySongsAsync(
                this.Songs.SkipWhile(song => song.Guid == this.SelectedSong.Guid).Select(y => y.Guid).ToList()));
            var playSongsMessage = this.PlaySongsCommand
                .Select(x => x.Status == ResponseStatus.Success ? "Playing songs" : "Error adding songs");

            this.AddToPlaylistCommand = ReactiveCommand.CreateAsyncTask(x => NetworkMessenger.Instance.AddSongToPlaylistAsync(this.SelectedSong.Guid));
            var addToPlaylistMessage = this.AddToPlaylistCommand
                .Select(x => x.Status == ResponseStatus.Success ? "Song added to playlist" : "Error adding song");

            this.Message = playSongsMessage.Merge(addToPlaylistMessage)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .ObserveOn(RxApp.MainThreadScheduler);
        }

        public ReactiveCommand<ResponseInfo> AddToPlaylistCommand { get; private set; }

        public IObservable<string> Message { get; private set; }

        public ReactiveCommand<ResponseInfo> PlaySongsCommand { get; private set; }

        public RemoteSong SelectedSong
        {
            get { return this.selectedSong; }
            set { this.RaiseAndSetIfChanged(ref this.selectedSong, value); }
        }

        public IReadOnlyList<RemoteSong> Songs { get; private set; }
    }
}