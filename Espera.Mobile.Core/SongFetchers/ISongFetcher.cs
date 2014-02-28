using Espera.Mobile.Core.Songs;
using System;
using System.Collections.Generic;

namespace Espera.Mobile.Core.SongFetchers
{
    public interface ISongFetcher<out T> where T : Song
    {
        IObservable<IReadOnlyList<T>> GetSongsAsync();
    }
}