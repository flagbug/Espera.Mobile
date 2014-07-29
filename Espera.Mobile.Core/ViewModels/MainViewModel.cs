using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Settings;
using Espera.Network;
using ReactiveMarrow;
using ReactiveUI;

namespace Espera.Mobile.Core.ViewModels
{
    public class MainViewModel : ReactiveObject, ISupportsActivation
    {
        private ObservableAsPropertyHelper<bool> isConnected;

        public MainViewModel(Func<string> ipAddress)
        {
            if (ipAddress == null)
                throw new ArgumentNullException("ipAddress");

            this.Activator = new ViewModelActivator();

            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                this.isConnected = NetworkMessenger.Instance.IsConnected
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .ToProperty(this, x => x.IsConnected)
                    .DisposeWith(disposable);

                var canConnect = this.WhenAnyValue(x => x.IsConnected, x => !x);
                this.ConnectCommand = ReactiveCommand.CreateAsyncObservable(canConnect, _ => ConnectAsync(ipAddress(), UserSettings.Instance.Port)
                    .Timeout(TimeSpan.FromSeconds(10), RxApp.TaskpoolScheduler)
                    .Catch<Unit, TimeoutException>(ex => Observable.Throw<Unit>(new Exception("Connection failed"))));

                this.DisconnectCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.IsConnected));
                this.DisconnectCommand.Subscribe(x => NetworkMessenger.Instance.Disconnect());

                this.ConnectionFailed = this.ConnectCommand.ThrownExceptions
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Select(x => x.Message);

                return disposable;
            });
        }

        public ViewModelActivator Activator { get; private set; }

        public ReactiveCommand<Unit> ConnectCommand { get; private set; }

        public IObservable<string> ConnectionFailed { get; private set; }

        public ReactiveCommand<object> DisconnectCommand { get; private set; }

        public bool IsConnected
        {
            get { return this.isConnected.Value; }
        }

        private static IObservable<Unit> ConnectAsync(string localAddress, int port)
        {
            if (localAddress == null)
                throw new Exception("You have to enable WiFi!");

            bool hasCustomIpAddress = !string.IsNullOrWhiteSpace(UserSettings.Instance.ServerAddress);

            return Observable.If(() => hasCustomIpAddress,
                    Observable.Return(UserSettings.Instance.ServerAddress),
                    NetworkMessenger.Instance.DiscoverServerAsync(localAddress, port))
                .SelectMany(async address =>
                {
                    string password = UserSettings.Instance.EnableAdministratorMode ? UserSettings.Instance.AdministratorPassword : null;

                    Tuple<ResponseStatus, ConnectionInfo> response = await NetworkMessenger.Instance
                        .ConnectAsync(address, port, UserSettings.Instance.UniqueIdentifier, password);

                    if (response.Item1 == ResponseStatus.WrongPassword)
                    {
                        throw new Exception("Password incorrect");
                    }

                    var minimumVersion = new Version("2.0.0");
                    if (response.Item2.ServerVersion < minimumVersion)
                    {
                        NetworkMessenger.Instance.Disconnect();
                        throw new Exception(string.Format("Espera version {0} required", minimumVersion.ToString(3)));
                    }

                    return Unit.Default;
                });
        }
    }
}