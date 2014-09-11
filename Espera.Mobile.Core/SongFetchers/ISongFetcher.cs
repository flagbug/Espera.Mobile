using System;
using System.Collections.Generic;
using Espera.Network;

namespace Espera.Mobile.Core.SongFetchers
{
    public interface ISongFetcher<out T> where T : NetworkSong
    {
        IObservable<IReadOnlyList<T>> GetSongsAsync();
    }
}