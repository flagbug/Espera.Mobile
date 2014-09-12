using System;
using System.Reactive.Linq;
using Espera.Network;
using ReactiveUI;
using Splat;

namespace Espera.Mobile.Core.ViewModels
{
    public class YoutubeSongViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<IBitmap> artwork;
        private readonly NetworkSong model;

        public YoutubeSongViewModel(NetworkSong model)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            this.model = model;

            this.artwork = Observable.FromAsync(() => ArtworkHelper.LoadArtwork(model))
                .LoggedCatch(this, null, "Failed to load YouTube artwork")
                .FirstAsync()
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, x => x.Artwork);
            var connectArtwork = this.Artwork;
        }

        public IBitmap Artwork
        {
            get { return this.artwork.Value; }
        }

        public Guid Guid
        {
            get { return this.model.Guid; }
        }

        public string Title
        {
            get { return this.model.Title; }
        }

        public string Uploader
        {
            get { return this.model.Artist; }
        }

        public int Views
        {
            get { return this.model.PlaybackCount; }
        }
    }
}