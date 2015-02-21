using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Espera.Mobile.Core.Network;
using Espera.Network;
using ReactiveMarrow;
using ReactiveUI;

namespace Espera.Mobile.Core.ViewModels
{
    public abstract class SongsViewModelBase<TSongViewModel> : ReactiveObject, ISupportsActivation
    {
        private ObservableAsPropertyHelper<bool> isAdmin;
        private ObservableAsPropertyHelper<int?> remainingVotes;
        private TSongViewModel selectedSong;
        private IReadOnlyList<TSongViewModel> songs;

        protected SongsViewModelBase()
        {
            this.Activator = new ViewModelActivator();

            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                this.remainingVotes = NetworkMessenger.Instance.WhenAnyValue(x => x.GuestSystemInfo)
                    .Select(x => x.IsEnabled ? new int?(x.RemainingVotes) : null)
                    .ToProperty(this, x => x.RemainingVotes)
                    .DisposeWith(disposable);

                this.isAdmin = NetworkMessenger.Instance.WhenAnyValue(x => x.AccessPermission, x => x == NetworkAccessPermission.Admin)
                    .ToProperty(this, x => x.IsAdmin)
                    .DisposeWith(disposable);

                return disposable;
            });
        }

        public ViewModelActivator Activator { get; private set; }

        public abstract ReactiveCommand<Unit> AddToPlaylistCommand { get; }

        public bool IsAdmin
        {
            get { return this.isAdmin.Value; }
        }

        public int? RemainingVotes
        {
            get { return this.remainingVotes.Value; }
        }

        public TSongViewModel SelectedSong
        {
            get { return this.selectedSong; }
            set { this.RaiseAndSetIfChanged(ref this.selectedSong, value); }
        }

        public IReadOnlyList<TSongViewModel> Songs
        {
            get { return this.songs; }
            protected set { this.RaiseAndSetIfChanged(ref this.songs, value); }
        }
    }
}