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
    public class SoundCloudViewModel : SongsViewModelBase<SoundCloudSongViewModel>
    {
        private ReactiveCommand<Unit> addToPlaylistCommand;
        private string searchTerm;

        public SoundCloudViewModel()
        {
            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                this.addToPlaylistCommand = ReactiveCommand.CreateAsyncObservable(_ =>
                    NetworkMessenger.Instance.AddSongToPlaylistAsync(this.SelectedSong.Guid).ToObservable().TakeUntil(disposable));

                this.LoadCommand = ReactiveCommand.CreateAsyncObservable(_ =>
                    NetworkMessenger.Instance.GetSoundCloudSongsAsync(this.SearchTerm).ToObservable()
                        .Select(x => (IReadOnlyList<SoundCloudSongViewModel>)x.Select(y => new SoundCloudSongViewModel(y)).ToList())
                        .TakeUntil(disposable));

                this.LoadCommand.Subscribe(x => this.Songs = x)
                    .DisposeWith(disposable);

                return disposable;
            });
        }

        public override ReactiveCommand<Unit> AddToPlaylistCommand
        {
            get { return this.addToPlaylistCommand; }
        }

        public ReactiveCommand<IReadOnlyList<SoundCloudSongViewModel>> LoadCommand { get; private set; }

        public string SearchTerm
        {
            get { return this.searchTerm; }
            set { this.RaiseAndSetIfChanged(ref this.searchTerm, value); }
        }
    }
}