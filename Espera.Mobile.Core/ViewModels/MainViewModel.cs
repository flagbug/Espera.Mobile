using Espera.Android.Network;
using Espera.Android.Settings;
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

        public MainViewModel()
        {
            this.isConnected = NetworkMessenger.Instance.IsConnected
                .ToProperty(this, x => x.IsConnected);

            this.ConnectCommand = this.WhenAnyValue(x => x.IsConnected, x => !x).ToCommand();
            this.ConnectCommand.RegisterAsync(x =>
                ConnectAsync(UserSettings.Instance.Port).ToObservable()
                    .Timeout(TimeSpan.FromSeconds(10), RxApp.TaskpoolScheduler)
                    .Catch<Unit, TimeoutException>(ex => Observable.Throw<Unit>(new Exception("Connection failed"))))
                .Subscribe();

            this.DisconnectCommand = this.WhenAnyValue(x => x.IsConnected).ToCommand();
            this.DisconnectCommand.Subscribe(x => NetworkMessenger.Instance.Disconnect());

            this.ConnectionFailed = this.ConnectCommand.ThrownExceptions
                .Select(x => x.Message);
        }

        public ReactiveCommand ConnectCommand { get; private set; }

        public IObservable<string> ConnectionFailed { get; private set; }

        public ReactiveCommand DisconnectCommand { get; private set; }

        public bool IsConnected
        {
            get { return this.isConnected.Value; }
        }

        private static async Task ConnectAsync(int port)
        {
            IPAddress address = await NetworkMessenger.DiscoverServer(port);

            string password = UserSettings.Instance.EnableAdministratorMode ? UserSettings.Instance.AdministratorPassword : null;

            if (UserSettings.Instance.UniqueIdentifier == null)
            {
                UserSettings.Instance.UniqueIdentifier = Guid.NewGuid().ToString();
            }

            ConnectionInfo connectionInfo = await NetworkMessenger.Instance.ConnectAsync(address, port, new Guid(UserSettings.Instance.UniqueIdentifier), password);

            if (connectionInfo.ResponseInfo.StatusCode != 200)
            {
                throw new Exception("Password incorrect");
            }

#if RELEASE
            var minimumVersion = new Version("2.0.0");
            if (connectionInfo.ServerVersion < minimumVersion)
            {
                NetworkMessenger.Instance.Disconnect();
                throw new Exception(string.Format("Espera version {0} required", minimumVersion.ToString(3)));
            }
#endif
        }
    }
}