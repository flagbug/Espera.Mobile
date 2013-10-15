using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Espera.Android
{
    public class SongsViewModel : ReactiveObject
    {
        public SongsViewModel(IReadOnlyList<Song> songs)
        {
            if (songs == null)
                throw new ArgumentNullException("songs");

            this.Songs = songs;

            this.PlaySongsCommand = new ReactiveCommand();
            var playSongsMessage = this.PlaySongsCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.PlaySongs(new[] { this.Songs[(int)x].Guid }))
                .Select(x => x.Item1 == 200 ? "Playing songs" : "Error adding songs");

            this.AddToPlaylistCommand = new ReactiveCommand();
            var addToPlaylistMessage = this.AddToPlaylistCommand.RegisterAsyncTask(x => NetworkMessenger.Instance.AddSongToPlaylist(this.Songs[(int)x].Guid))
                .Select(x => x.Item1 == 200 ? "Song added to playlist" : "Error adding song");

            this.Message = playSongsMessage.Merge(addToPlaylistMessage)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .ObserveOn(RxApp.MainThreadScheduler);
        }

        public ReactiveCommand AddToPlaylistCommand { get; private set; }

        public IObservable<string> Message { get; private set; }

        public ReactiveCommand PlaySongsCommand { get; private set; }

        public IReadOnlyList<Song> Songs { get; private set; }
    }
}