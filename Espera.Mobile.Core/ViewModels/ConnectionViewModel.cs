using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Settings;
using ReactiveMarrow;
using ReactiveUI;

namespace Espera.Mobile.Core.ViewModels
{
    public class ConnectionViewModel : ReactiveObject, ISupportsActivation
    {
        public static readonly TimeSpan ConnectCommandTimeout = TimeSpan.FromSeconds(10);

        private readonly IClock clock;
        private readonly IInstallationDateFetcher installationDateFetcher;
        private readonly UserSettings userSettings;
        private ObservableAsPropertyHelper<bool> isConnected;

        public ConnectionViewModel(UserSettings userSettings, Func<string> ipAddress, IInstallationDateFetcher installationDateFetcher = null, IClock clock = null)
        {
            if (ipAddress == null)
                throw new ArgumentNullException("ipAddress");

            if (userSettings == null)
                throw new ArgumentNullException("userSettings");

            this.userSettings = userSettings;
            this.installationDateFetcher = installationDateFetcher;
            this.clock = clock;

            this.Activator = new ViewModelActivator();

            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                this.isConnected = NetworkMessenger.Instance.WhenAnyValue(x => x.IsConnected)
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
                this.ConnectCommand = ReactiveCommand.CreateAsyncObservable(canConnect, _ => ConnectAsync(ipAddress(), this.userSettings.Port)
                    .Timeout(ConnectCommandTimeout, RxApp.TaskpoolScheduler)
                    .Catch<ConnectionResultContainer, TimeoutException>(ex => Observable.Return(new ConnectionResultContainer(ConnectionResult.Timeout)))
                    .Catch<ConnectionResultContainer, NetworkException>(ex => Observable.Return(new ConnectionResultContainer(ConnectionResult.Failed)))
                    .TakeUntil(connectionInterrupt));

                this.DisconnectCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.IsConnected));
                this.DisconnectCommand.Subscribe(x => NetworkMessenger.Instance.Disconnect());

                // We don't simply use InvokeCommand here, it results in a wierd infinite loop where
                // DisconnectCommand is executed infinitely
                this.ConnectCommand.Where(x => x.ConnectionResult != ConnectionResult.Successful)
                    .SelectMany(x => this.DisconnectCommand.CanExecute(null) ? this.DisconnectCommand.ExecuteAsync() : Observable.Return((object)null))
                    .Subscribe()
                    .DisposeWith(disposable);

                return disposable;
            });
        }

        public ViewModelActivator Activator { get; private set; }

        public ReactiveCommand<ConnectionResultContainer> ConnectCommand { get; private set; }

        public ReactiveCommand<object> DisconnectCommand { get; private set; }

        public bool IsConnected
        {
            get { return this.isConnected.Value; }
        }

        private IObservable<ConnectionResultContainer> ConnectAsync(string localAddress, int port)
        {
            if (localAddress == null)
                return Observable.Return(new ConnectionResultContainer(ConnectionResult.WifiDisabled));

            bool hasCustomIpAddress = !string.IsNullOrWhiteSpace(this.userSettings.ServerAddress);

            return Observable.If(() => hasCustomIpAddress,
                    Observable.Return(this.userSettings.ServerAddress),
                    NetworkMessenger.Instance.DiscoverServerAsync(localAddress, port))
                .SelectMany(address =>
                {
                    string password = null;

                    // We don't want users that aren't premium or in the trial period to send any
                    // existing password
                    if (this.userSettings.IsPremium || TrialHelpers.IsInTrialPeriod(AppConstants.TrialTime, this.clock, this.installationDateFetcher))
                    {
                        password = string.IsNullOrWhiteSpace(this.userSettings.AdministratorPassword) ?
                             null : this.userSettings.AdministratorPassword;
                    }

                    return NetworkMessenger.Instance.ConnectAsync(address, port, this.userSettings.UniqueIdentifier, password);
                });
        }
    }
}