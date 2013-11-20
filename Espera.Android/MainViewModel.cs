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
                ConnectAsync(port.FirstAsync().Wait()).ToObservable().Timeout(TimeSpan.FromSeconds(10), RxApp.TaskpoolScheduler));

            this.ConnectionFailed = this.ConnectCommand.ThrownExceptions
                .Select(x => Unit.Default);

            this.isConnected = NetworkMessenger.Instance.IsConnected
                .ToProperty(this, x => x.IsConnected);

            port.DistinctUntilChanged()
                .CombineLatestValue(NetworkMessenger.Instance.IsConnected, (p, connected) => connected)
                .Where(x => x)
                .Subscribe(x => NetworkMessenger.Instance.Disconnect());
        }

        public ReactiveCommand ConnectCommand { get; private set; }

        public IObservable<Unit> ConnectionFailed { get; private set; }

        public bool EnableAdministratorMode { get; set; }

        public bool IsConnected
        {
            get { return this.isConnected.Value; }
        }

        public string Password { get; set; }

        private async Task ConnectAsync(int port)
        {
            IPAddress address = await NetworkMessenger.DiscoverServer(port);

            await NetworkMessenger.Instance.ConnectAsync(address, port);

            if (this.EnableAdministratorMode)
            {
                Tuple<int, string> response = await NetworkMessenger.Instance.Authorize(this.Password);

                if (response.Item1 != 200)
                {
                    throw new Exception("Wrong password");
                }
            }
        }
    }
}