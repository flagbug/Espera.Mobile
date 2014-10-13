using System;
using Espera.Network;
using ReactiveUI;

namespace Espera.Mobile.Core.ViewModels
{
    public class RemoteSongViewModel : ReactiveObject
    {
        private readonly NetworkSong model;

        public RemoteSongViewModel(NetworkSong model)
        {
            if (model == null)
                throw new ArgumentNullException("model");

            this.model = model;
        }

        public string Artist
        {
            get { return this.model.Artist; }
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