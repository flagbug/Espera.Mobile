using Espera.Network;
using ReactiveUI;
using System;

namespace Espera.Mobile.Core.ViewModels
{
    public class PlaylistEntryViewModel : ReactiveObject
    {
        private readonly NetworkSong song;
        private bool isPlayling;

        public PlaylistEntryViewModel(NetworkSong song, bool isPlaying = false)
        {
            if (song == null)
                throw new ArgumentNullException("song");

            this.song = song;
            this.isPlayling = isPlaying;
        }

        public string Artist
        {
            get { return song.Source == NetworkSongSource.Local ? song.Artist : "YouTube"; }
        }

        public Guid Guid
        {
            get { return this.song.Guid; }
        }

        public bool IsPlaying
        {
            get { return this.isPlayling; }
            set { this.RaiseAndSetIfChanged(ref this.isPlayling, value); }
        }

        public string Title
        {
            get { return this.song.Title; }
        }

        public override int GetHashCode()
        {
            return new { Guid }.GetHashCode();
        }
    }
}