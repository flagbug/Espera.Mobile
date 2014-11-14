using Espera.Network;
using ReactiveUI;
using System;

namespace Espera.Mobile.Core.ViewModels
{
    public class RemoteSongViewModel : ReactiveObject
    {
        private readonly NetworkSong model;

        public RemoteSongViewModel(NetworkSong model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

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

        public NetworkSong Model
        {
            get { return this.model; }
        }

        public string Title
        {
            get { return this.model.Title; }
        }
    }
}