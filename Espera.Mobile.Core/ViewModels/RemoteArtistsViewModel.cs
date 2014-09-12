using System;
using System.Collections.Generic;
using Akavache;
using Espera.Mobile.Core.SongFetchers;
using Espera.Network;

namespace Espera.Mobile.Core.ViewModels
{
    public class RemoteArtistsViewModel : ArtistsViewModel<NetworkSong>
    {
        public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

        public RemoteArtistsViewModel(ISongFetcher<NetworkSong> songFetcher = null)
            : base(new CachingSongFetcher<NetworkSong>(songFetcher ?? new RemoteSongFetcher(), BlobCacheKeys.RemoteSongs, CacheDuration), BlobCacheKeys.SelectedRemoteSongs)
        { }

        private class CachingSongFetcher<T> : ISongFetcher<T> where T : NetworkSong
        {
            private readonly TimeSpan cacheDuration;
            private readonly string cacheKey;
            private readonly ISongFetcher<T> wrappedFetcher;

            public CachingSongFetcher(ISongFetcher<T> wrappedFetcher, string cacheKey, TimeSpan cacheDuration)
            {
                if (wrappedFetcher == null)
                    throw new ArgumentNullException("wrappedFetcher");

                this.wrappedFetcher = wrappedFetcher;
                this.cacheKey = cacheKey;
                this.cacheDuration = cacheDuration;
            }

            public IObservable<IReadOnlyList<T>> GetSongsAsync()
            {
                return BlobCache.LocalMachine.GetAndFetchLatest(this.cacheKey, () => this.wrappedFetcher.GetSongsAsync(), null, DateTimeOffset.Now + cacheDuration);
            }
        }
    }
}