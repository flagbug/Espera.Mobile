using System;
using Espera.Network;
using ReactiveUI;

namespace Espera.Mobile.Core.ViewModels
{
    public class PlaylistEntryViewModel : ReactiveObject
    {
        private readonly NetworkSong song;
        private bool isPlayling;

        public PlaylistEntryViewModel(NetworkSong song, bool isVoteable, bool isPlaying = false)
        {
            if (song == null)
                throw new ArgumentNullException("song");

            this.song = song;
            this.IsVoteAble = isVoteable;
            this.isPlayling = isPlaying;
        }

        public string Artist
        {
            get
            {
                if (song.Source == NetworkSongSource.Youtube)
                {
                    return "YouTube";
                }

                return song.Artist;
            }
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

        public bool IsVoteAble { get; private set; }

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