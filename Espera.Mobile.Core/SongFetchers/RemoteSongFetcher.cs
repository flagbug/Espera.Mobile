using Espera.Mobile.Core.Network;
using Espera.Network;
using System;
using System.Collections.Generic;
using System.Reactive.Threading.Tasks;

namespace Espera.Mobile.Core.SongFetchers
{
    public class RemoteSongFetcher : ISongFetcher<NetworkSong>
    {
        public IObservable<IReadOnlyList<NetworkSong>> GetSongsAsync() => NetworkMessenger.Instance.GetSongsAsync().ToObservable();
    }
}