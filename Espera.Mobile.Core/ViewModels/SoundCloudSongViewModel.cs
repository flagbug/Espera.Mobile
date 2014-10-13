using System;
using System.Reactive.Linq;
using Espera.Network;
using ReactiveUI;
using Splat;

namespace Espera.Mobile.Core.ViewModels
{
    public class SoundCloudSongViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<IBitmap> artwork;
        private readonly NetworkSong model;

        public SoundCloudSongViewModel(NetworkSong model)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            this.model = model;

            this.artwork = ArtworkHelper.LoadArtwork(model)
                .LoggedCatch(this, null, "Failed to load SoundCloud artwork")
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.Artwork);
            var connectArtwork = this.Artwork;
        }

        public string Artist
        {
            get { return this.model.Artist; }
        }

        public IBitmap Artwork
        {
            get { return this.artwork.Value; }
        }

        public TimeSpan Duration
        {
            get { return this.model.Duration; }
        }

        public Guid Guid
        {
            get { return this.model.Guid; }
        }

        public int PlaybackCount
        {
            get { return this.model.PlaybackCount; }
        }

        public string Title
        {
            get { return this.model.Title; }
        }
    }
}