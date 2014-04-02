using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Settings;
using Espera.Network;
using ReactiveUI;
using System;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Espera.Mobile.Core.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<bool> isConnected;

        public MainViewModel()
        {
            this.isConnected = NetworkMessenger.Instance.IsConnected
                .ToProperty(this, x => x.IsConnected);

            var canConnect = this.WhenAnyValue(x => x.IsConnected, x => !x);
            this.ConnectCommand = ReactiveCommand.Create(canConnect, _ => ConnectAsync(UserSettings.Instance.Port).ToObservable()
                    .Timeout(TimeSpan.FromSeconds(10), RxApp.TaskpoolScheduler)
                    .Catch<Unit, TimeoutException>(ex => Observable.Throw<Unit>(new Exception("Connection failed"))));

            this.DisconnectCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.IsConnected));
            this.DisconnectCommand.Subscribe(x => NetworkMessenger.Instance.Disconnect());

            this.ConnectionFailed = this.ConnectCommand.ThrownExceptions
                .Select(x => x.Message);
        }

        public ReactiveCommand<Unit> ConnectCommand { get; private set; }

        public IObservable<string> ConnectionFailed { get; private set; }

        public ReactiveCommand<object> DisconnectCommand { get; private set; }

        public bool IsConnected
        {
            get { return this.isConnected.Value; }
        }

        private static async Task ConnectAsync(int port)
        {
            IPAddress address = await NetworkMessenger.DiscoverServer(port);

            string password = UserSettings.Instance.EnableAdministratorMode ? UserSettings.Instance.AdministratorPassword : null;

            Tuple<ResponseStatus, ConnectionInfo> response = await NetworkMessenger.Instance
                .ConnectAsync(address, port, new Guid(UserSettings.Instance.UniqueIdentifier), password);

            if (response.Item1 == ResponseStatus.WrongPassword)
            {
                throw new Exception("Password incorrect");
            }

#if !DEBUG
            var minimumVersion = new Version("2.0.0");
            if (response.Item2.ServerVersion < minimumVersion)
            {
                NetworkMessenger.Instance.Disconnect();
                throw new Exception(string.Format("Espera version {0} required", minimumVersion.ToString(3)));
            }
#endif
        }
    }
}