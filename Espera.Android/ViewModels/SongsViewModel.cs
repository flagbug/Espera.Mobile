using Espera.Android.Network;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace Espera.Android.ViewModels
{
    public class SongsViewModel : ReactiveObject
    {
        public SongsViewModel(IReadOnlyReactiveList<Song> songs)
        {
            if (songs == null)
                throw new ArgumentNullException("songs");

            this.Songs = songs;

            this.PlaySongsCommand = new ReactiveCommand();
            var playSongsMessage = this.PlaySongsCommand.RegisterAsyncTask(x =>
                    NetworkMessenger.Instance.PlaySongs(this.Songs.Skip((int)x).Select(y => y.Guid)))
                .Select(x => x.StatusCode == 200 ? "Playing songs" : "Error adding songs")
                .Publish().PermaRef();

            this.AddToPlaylistCommand = new ReactiveCommand();
            var addToPlaylistMessage = this.AddToPlaylistCommand.RegisterAsyncTask(x =>
                    NetworkMessenger.Instance.AddSongToPlaylist(this.Songs[(int)x].Guid))
                .Select(x => x.StatusCode == 200 ? "Song added to playlist" : "Error adding song")
                .Publish().PermaRef();

            this.Message = playSongsMessage.Merge(addToPlaylistMessage)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .ObserveOn(RxApp.MainThreadScheduler);
        }

        public ReactiveCommand AddToPlaylistCommand { get; private set; }

        public IObservable<string> Message { get; private set; }

        public ReactiveCommand PlaySongsCommand { get; private set; }

        public IReadOnlyReactiveList<Song> Songs { get; private set; }
    }
}