﻿using System;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Espera.Network;
using Fusillade;
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

            this.artwork = Observable.FromAsync(this.LoadArtwork)
                .FirstAsync()
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

        public Guid Guid
        {
            get { return this.model.Guid; }
        }

        public string Title
        {
            get { return this.model.Title; }
        }

        private async Task<IBitmap> LoadArtwork()
        {
            if (this.model.ArtworkKey == null)
                return null;

            using (var client = new HttpClient(NetCache.UserInitiated))
            {
                try
                {
                    var stream = await client.GetStreamAsync(this.model.ArtworkKey);

                    return await BitmapLoader.Current.Load(stream, null, null);
                }

                catch (Exception ex)
                {
                    this.Log().ErrorException("Failed to load SoundCloud artwork", ex);

                    return null;
                }
            }
        }
    }
}