using Espera.Mobile.Core.Network;
using Espera.Network;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace Espera.Mobile.Core.ViewModels
{
    public class RemoteSongsViewModel : ReactiveObject
    {
        public RemoteSongsViewModel(IReadOnlyReactiveList<NetworkSong> songs)
        {
            if (songs == null)
                throw new ArgumentNullException("songs");

            this.Songs = songs;

            this.PlaySongsCommand = ReactiveCommand.CreateAsync(x => NetworkMessenger.Instance.PlaySongsAsync(this.Songs.Skip((int)x).Select(y => y.Guid)));
            var playSongsMessage = this.PlaySongsCommand
                .Select(x => x.Status == ResponseStatus.Success ? "Playing songs" : "Error adding songs");

            this.AddToPlaylistCommand = ReactiveCommand.CreateAsync(x => NetworkMessenger.Instance.AddSongToPlaylistAsync(this.Songs[(int)x].Guid));
            var addToPlaylistMessage = this.AddToPlaylistCommand
                .Select(x => x.Status == ResponseStatus.Success ? "Song added to playlist" : "Error adding song");

            this.Message = playSongsMessage.Merge(addToPlaylistMessage)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .ObserveOn(RxApp.MainThreadScheduler);
        }

        public ReactiveCommand<ResponseInfo> AddToPlaylistCommand { get; private set; }

        public IObservable<string> Message { get; private set; }

        public ReactiveCommand<ResponseInfo> PlaySongsCommand { get; private set; }

        public IReadOnlyReactiveList<NetworkSong> Songs { get; private set; }
    }
}