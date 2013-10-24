using ReactiveUI;
using System;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Espera.Android
{
    public class MainViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<bool> isConnected;

        public MainViewModel(IObservable<int> port)
        {
            this.ConnectCommand = new ReactiveCommand();
            this.ConnectCommand.RegisterAsync(x => 
                ConnectAsync(port.FirstAsync().Wait()).ToObservable().Timeout(TimeSpan.FromSeconds(10)));
            this.ConnectionFailed = this.ConnectCommand.ThrownExceptions.Select(x => Unit.Default);

            this.isConnected = NetworkMessenger.Instance.IsConnected
                .ToProperty(this, x => x.IsConnected);

            port.DistinctUntilChanged()
                .CombineLatestValue(NetworkMessenger.Instance.IsConnected, Tuple.Create)
                .Where(x => x.Item2)
                .Subscribe(x => NetworkMessenger.Instance.Disconnect());
        }

        public ReactiveCommand ConnectCommand { get; private set; }

        public IObservable<Unit> ConnectionFailed { get; private set; }

        public bool IsConnected
        {
            get { return this.isConnected.Value; }
        }

        private static async Task ConnectAsync(int port)
        {
            IPAddress address = await NetworkMessenger.DiscoverServer(port);

            await NetworkMessenger.Instance.ConnectAsync(address, port);
        }
    }
}