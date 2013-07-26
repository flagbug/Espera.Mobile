using ReactiveUI;
using System.Reactive.Linq;

namespace Espera.Android
{
    public class MainViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<string> ipAdress;

        public MainViewModel()
        {
            this.DiscoverServerCommand = new ReactiveCommand();
            this.ipAdress = this.DiscoverServerCommand.RegisterAsyncTask(_ => NetworkMessenger.DiscoverServer())
                .Do(x => new NetworkMessenger(x).ConnectAsync())
                .Select(x => x.ToString())
                .ToProperty(this, x => x.IpAddress);
        }

        public IReactiveCommand DiscoverServerCommand { get; private set; }

        public string IpAddress
        {
            get { return this.ipAdress.Value; }
        }
    }
}