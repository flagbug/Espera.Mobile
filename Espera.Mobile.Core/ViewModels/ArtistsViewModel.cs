using Akavache;
using Espera.Mobile.Core.SongFetchers;
using Espera.Network;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Espera.Mobile.Core.ViewModels
{
    public class ArtistsViewModel<T> : ReactiveObject, ISupportsActivation where T : NetworkSong
    {
        public static readonly TimeSpan LoadCommandTimeout = TimeSpan.FromSeconds(15);

        private ObservableAsPropertyHelper<IReadOnlyList<string>> artists;
        private string selectedArtist;
        private IReadOnlyList<T> songs;

        public ArtistsViewModel(ISongFetcher<T> songFetcher, string serializationKey)
        {
            if (songFetcher == null)
                throw new ArgumentNullException(nameof(songFetcher));

            this.Activator = new ViewModelActivator();

            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                this.LoadCommand = ReactiveCommand.CreateAsyncObservable(_ => songFetcher.GetSongsAsync()
                    .Timeout(LoadCommandTimeout, RxApp.TaskpoolScheduler)
                    .TakeUntil(disposable));

                this.artists = this.LoadCommand
                   .Do(x => this.songs = x)
                   .Select(GetArtists)
                   .ToProperty(this, x => x.Artists, new List<string>());

                this.WhenAnyValue(x => x.SelectedArtist).Where(x => x != null)
                    .Select(FilterSongsByArtist)
                    .Select(x => BlobCache.LocalMachine.InsertObject(serializationKey, x))
                    .Concat()
                   .Subscribe();

                return disposable;
            });
        }

        public ViewModelActivator Activator { get; }

        public IReadOnlyList<string> Artists
        {
            get { return this.artists.Value; }
        }

        public ReactiveCommand<IReadOnlyList<T>> LoadCommand { get; private set; }

        public string SelectedArtist
        {
            get { return this.selectedArtist; }
            set { this.RaiseAndSetIfChanged(ref this.selectedArtist, value); }
        }

        private static IReadOnlyList<string> GetArtists(IEnumerable<T> songs)
        {
            return songs.GroupBy(s => s.Artist)
                .Select(g => g.Key)
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(SortHelpers.RemoveArtistPrefixes)
                .ToList();
        }

        private IEnumerable<T> FilterSongsByArtist(string artist)
        {
            return this.songs
                .Where(x => x.Artist.Equals(this.SelectedArtist, StringComparison.CurrentCultureIgnoreCase))
                .ToList();
        }
    }
}