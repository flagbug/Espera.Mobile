using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Espera.Mobile.Core.Network;
using ReactiveMarrow;
using ReactiveUI;

namespace Espera.Mobile.Core.ViewModels
{
    public class YoutubeViewModel : SongsViewModelBase<YoutubeSongViewModel>
    {
        private ReactiveCommand<Unit> addToPlaylistCommand;
        private string searchTerm;

        public YoutubeViewModel()
        {
            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                this.addToPlaylistCommand = ReactiveCommand.CreateAsyncObservable(_ =>
                    NetworkMessenger.Instance.AddSongToPlaylistAsync(this.SelectedSong.Guid).ToObservable().TakeUntil(disposable));

                this.LoadCommand = ReactiveCommand.CreateAsyncObservable(_ =>
                    NetworkMessenger.Instance.GetYoutubeSongsAsync(this.SearchTerm).ToObservable()
                    .Select(x => (IReadOnlyList<YoutubeSongViewModel>)x.Select(y => new YoutubeSongViewModel(y)).ToList()));

                this.LoadCommand.Subscribe(x => this.Songs = x)
                    .DisposeWith(disposable);

                return disposable;
            });
        }

        public override ReactiveCommand<Unit> AddToPlaylistCommand
        {
            get { return this.addToPlaylistCommand; }
        }

        public ReactiveCommand<IReadOnlyList<YoutubeSongViewModel>> LoadCommand { get; private set; }

        public string SearchTerm
        {
            get { return this.searchTerm; }
            set { this.RaiseAndSetIfChanged(ref this.searchTerm, value); }
        }
    }
}