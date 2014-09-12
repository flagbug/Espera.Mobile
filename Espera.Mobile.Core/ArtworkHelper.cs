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
        public static async Task<IBitmap> LoadArtwork(NetworkSong song)
        {
            if (song.ArtworkKey == null)
                return null;

            byte[] imageBytes = await BlobCache.LocalMachine.GetOrFetchObject(song.ArtworkKey, () =>
            {
                using (var client = new HttpClient(NetCache.UserInitiated))
                {
                    return client.GetByteArrayAsync(song.ArtworkKey);
                }
            }, DateTimeOffset.Now + TimeSpan.FromDays(1));

            using (var stream = new MemoryStream(imageBytes))
            {
                return await BitmapLoader.Current.Load(stream, null, null);
            }
        }
    }
}