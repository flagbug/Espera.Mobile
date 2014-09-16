using System;
using System.IO;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Akavache;
using Espera.Network;
using Fusillade;
using Splat;

namespace Espera.Mobile.Core
{
    public static class ArtworkHelper
    {
        public static IObservable<IBitmap> LoadArtwork(NetworkSong song)
        {
            if (song.ArtworkKey == null)
                return Observable.Return((IBitmap)null);

            return BlobCache.LocalMachine.GetOrFetchObject(song.ArtworkKey, () => GetData(song.ArtworkKey), DateTimeOffset.Now + TimeSpan.FromDays(1))
                .SelectMany(async imageData =>
                {
                    using (var stream = new MemoryStream(imageData))
                    {
                        return await BitmapLoader.Current.Load(stream, null, null);
                    }
                });
        }

        private static IObservable<byte[]> GetData(string requestUrl)
        {
            return Observable.Using(() => new HttpClient(NetCache.UserInitiated),
                client => Observable.FromAsync(ct => client.GetAsync(requestUrl, ct)))
                    .SelectMany(message => message.Content.ReadAsByteArrayAsync());
        }
    }
}