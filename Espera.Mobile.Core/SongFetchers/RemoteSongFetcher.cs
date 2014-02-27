using Espera.Mobile.Core.Network;
using Espera.Network;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace Espera.Mobile.Core.SongFetchers
{
    public class RemoteSongFetcher : ISongFetcher
    {
        public IObservable<IReadOnlyList<NetworkSong>> GetSongsAsync()
        {
            return NetworkMessenger.Instance.GetSongsAsync().ToObservable()
                .Timeout(TimeSpan.FromSeconds(15), RxApp.TaskpoolScheduler);
        }
    }
}