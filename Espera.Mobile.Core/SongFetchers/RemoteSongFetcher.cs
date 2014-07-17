using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Songs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace Espera.Mobile.Core.SongFetchers
{
    public class RemoteSongFetcher : ISongFetcher<RemoteSong>
    {
        public IObservable<IReadOnlyList<RemoteSong>> GetSongsAsync()
        {
            return NetworkMessenger.Instance.GetSongsAsync().ToObservable()
                .Select(x => x.Select(RemoteSong.FromNetworkSong).ToList());
        }
    }
}