using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Settings;
using Espera.Network;
using ReactiveMarrow;
using ReactiveUI;

namespace Espera.Mobile.Core.ViewModels
{
    public class MainViewModel : ReactiveObject, ISupportsActivation
    {
        public static readonly TimeSpan ConnectCommandTimeout = TimeSpan.FromSeconds(10);
        public static readonly Version MinimumServerVersion = new Version("2.4.0");

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

                // We use this interrupt to make sure the application doesn't die if the ViewModel
                // is deactivated but the connect command throws an exception after that.
                //
                // We can't simply dispose the ReactiveCommand, as this only disposes the CanExecute subscription
                var connectionInterrupt = new AsyncSubject<Unit>();
                Disposable.Create(() =>
                {
                    connectionInterrupt.OnNext(Unit.Default);
                    connectionInterrupt.OnCompleted();
                }).DisposeWith(disposable);

                var canConnect = this.WhenAnyValue(x => x.IsConnected, x => !x);
                this.ConnectCommand = ReactiveCommand.CreateAsyncObservable(canConnect, _ => ConnectAsync(ipAddress(), UserSettings.Instance.Port)
                    .Timeout(ConnectCommandTimeout, RxApp.TaskpoolScheduler)
                    .Catch<Unit, TimeoutException>(ex => Observable.Throw<Unit>(new Exception("Connection timeout")))
                    .Catch<Unit, NetworkException>(ex => Observable.Throw<Unit>(new Exception("Connection failed")))
                    .TakeUntil(connectionInterrupt));

                this.DisconnectCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.IsConnected));
                this.DisconnectCommand.Subscribe(x => NetworkMessenger.Instance.Disconnect());

                this.ConnectCommand.ThrownExceptions.InvokeCommand(this.DisconnectCommand);

                this.ConnectionFailed = this.ConnectCommand.ThrownExceptions
                    .Select(x => x.Message)
                    .Merge(this.WhenAnyValue(x => x.IsConnected).Where(x => x).CombineLatest(NetworkMessenger.Instance.AccessPermission,
                        (connected, permission) => permission == NetworkAccessPermission.Admin ? "Connected as administrator" : "Connected as guest")
                        .TakeUntil(connectionInterrupt))
                    .ObserveOn(RxApp.MainThreadScheduler);

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
                        throw new WrongPasswordException("Password incorrect");
                    }

                    if (response.Item2.ServerVersion < MinimumServerVersion)
                    {
                        throw new ServerVersionException(string.Format("Espera version {0} required", MinimumServerVersion.ToString(3)));
                    }

                    return Unit.Default;
                });
        }
    }
}