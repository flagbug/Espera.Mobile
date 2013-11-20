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

            this.ConnectAsAdminCommand = new ReactiveCommand();
            this.WrongPassword = this.ConnectAsAdminCommand.RegisterAsync(x =>
                ConnectAsAdminAsync(port.FirstAsync().Wait(), this.Password).ToObservable().Timeout(TimeSpan.FromSeconds(10), RxApp.TaskpoolScheduler))
                .Where(x => x.Item1 != 200)
                .Select(_ => Unit.Default);

            this.ConnectionFailed = this.ConnectCommand.ThrownExceptions
                .Merge(this.ConnectAsAdminCommand.ThrownExceptions)
                .Select(x => Unit.Default);

            this.isConnected = NetworkMessenger.Instance.IsConnected
                .ToProperty(this, x => x.IsConnected);

            port.DistinctUntilChanged()
                .CombineLatestValue(NetworkMessenger.Instance.IsConnected, (p, connected) => connected)
                .Where(x => x)
                .Subscribe(x => NetworkMessenger.Instance.Disconnect());
        }

        public ReactiveCommand ConnectAsAdminCommand { get; private set; }

        public ReactiveCommand ConnectCommand { get; private set; }

        public IObservable<Unit> ConnectionFailed { get; private set; }

        public bool IsConnected
        {
            get { return this.isConnected.Value; }
        }

        public string Password { get; set; }

        public IObservable<Unit> WrongPassword { get; private set; }

        private static async Task<Tuple<int, string>> ConnectAsAdminAsync(int port, string password)
        {
            await ConnectAsync(port);

            return await NetworkMessenger.Instance.Authorize(password);
        }

        private static async Task ConnectAsync(int port)
        {
            IPAddress address = await NetworkMessenger.DiscoverServer(port);

            await NetworkMessenger.Instance.ConnectAsync(address, port);
        }
    }
}