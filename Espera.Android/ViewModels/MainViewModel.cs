using Espera.Android.Network;
using ReactiveUI;
using System;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Espera.Android.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<bool> isConnected;

        public MainViewModel(IObservable<int> port)
        {
            this.ConnectCommand = new ReactiveCommand();
            this.ConnectCommand.RegisterAsync(x =>
                ConnectAsync(port.FirstAsync().Wait()).ToObservable()
                    .Timeout(TimeSpan.FromSeconds(10), RxApp.TaskpoolScheduler)
                    .Catch<Unit, TimeoutException>(ex => Observable.Throw<Unit>(new Exception("Connection failed"))));

            this.ConnectionFailed = this.ConnectCommand.ThrownExceptions
                .Select(x => x.Message);

            this.isConnected = NetworkMessenger.Instance.IsConnected
                .CombineLatest(this.ConnectCommand.IsExecuting, (isConnected, isExecuting) => isConnected && !isExecuting)
                .ToProperty(this, x => x.IsConnected);

            port.DistinctUntilChanged()
                .CombineLatestValue(NetworkMessenger.Instance.IsConnected, (p, connected) => connected)
                .Where(x => x)
                .Subscribe(x => NetworkMessenger.Instance.Disconnect());
        }

        public ReactiveCommand ConnectCommand { get; private set; }

        public IObservable<string> ConnectionFailed { get; private set; }

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

            Version version = await NetworkMessenger.Instance.GetServerVersion();

            var minimumVersion = new Version("2.0.0");
            if (version < minimumVersion)
            {
                NetworkMessenger.Instance.Disconnect();
                throw new Exception(string.Format("Espera version {0} required", minimumVersion.ToString(3)));
            }

            if (this.EnableAdministratorMode)
            {
                ResponseInfo response = await NetworkMessenger.Instance.Authorize(this.Password);

                if (response.StatusCode != 200)
                {
                    throw new Exception("Password incorrect");
                }
            }
        }
    }
}