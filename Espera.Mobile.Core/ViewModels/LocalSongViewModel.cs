using System;
using ReactiveUI;

namespace Espera.Mobile.Core.ViewModels
{
    public class LocalSongViewModel : ReactiveObject
    {
        private readonly LocalSong model;
        private bool isTransfering;
        private int transferProgress;

        public LocalSongViewModel(LocalSong model)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            this.model = model;
        }

        public string Album
        {
            get { return this.model.Album; }
        }

        public TimeSpan Duration
        {
            get { return this.model.Duration; }
        }

        public bool IsTransfering
        {
            get { return this.isTransfering; }
            set { this.RaiseAndSetIfChanged(ref this.isTransfering, value); }
        }

        public LocalSong Model
        {
            get { return this.model; }
        }

        public string Path
        {
            get { return this.model.Path; }
        }

        public string Title
        {
            get { return this.model.Title; }
        }

        public int TransferProgress
        {
            get { return this.transferProgress; }
            set { this.RaiseAndSetIfChanged(ref this.transferProgress, value); }
        }
    }
}