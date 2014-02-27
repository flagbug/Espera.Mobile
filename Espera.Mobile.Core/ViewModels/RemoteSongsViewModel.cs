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

            this.PlaySongsCommand = new ReactiveCommand();
            var playSongsMessage = this.PlaySongsCommand.RegisterAsyncTask(x =>
                    NetworkMessenger.Instance.PlaySongsAsync(this.Songs.Skip((int)x).Select(y => y.Guid)))
                .Select(x => x.Status == ResponseStatus.Success ? "Playing songs" : "Error adding songs")
                .Publish().PermaRef();

            this.AddToPlaylistCommand = new ReactiveCommand();
            var addToPlaylistMessage = this.AddToPlaylistCommand.RegisterAsyncTask(x =>
                    NetworkMessenger.Instance.AddSongToPlaylistAsync(this.Songs[(int)x].Guid))
                .Select(x => x.Status == ResponseStatus.Success ? "Song added to playlist" : "Error adding song")
                .Publish().PermaRef();

            this.Message = playSongsMessage.Merge(addToPlaylistMessage)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .ObserveOn(RxApp.MainThreadScheduler);
        }

        public ReactiveCommand AddToPlaylistCommand { get; private set; }

        public IObservable<string> Message { get; private set; }

        public ReactiveCommand PlaySongsCommand { get; private set; }

        public IReadOnlyReactiveList<NetworkSong> Songs { get; private set; }
    }
}