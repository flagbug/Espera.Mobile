using Espera.Mobile.Core.Network;
using System;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;
using Espera.Network;

namespace Espera.Mobile.Core.SongFetchers
{
    public class RemoteSongFetcher : ISongFetcher<NetworkSong>
    {
        public IObservable<IReadOnlyList<NetworkSong>> GetSongsAsync()
        {
            return NetworkMessenger.Instance.GetSongsAsync().ToObservable();
        }
    }
}