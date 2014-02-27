using Espera.Network;
using System;
using System.Collections.Generic;

namespace Espera.Mobile.Core.SongFetchers
{
    public interface ISongFetcher
    {
        IObservable<IReadOnlyList<NetworkSong>> GetSongsAsync();
    }
}