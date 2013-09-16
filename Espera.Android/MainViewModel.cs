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

        public MainViewModel()
        {
            this.ConnectCommand = new ReactiveCommand();
            this.isConnected = this.ConnectCommand
                .RegisterAsync(x => ConnectAsync().ToObservable().Timeout(TimeSpan.FromSeconds(10)))
                .Select(x => true)
                .ToProperty(this, x => x.IsConnected);

            this.ConnectionFailed = this.ConnectCommand.ThrownExceptions.Select(x => Unit.Default);
        }

        public ReactiveCommand ConnectCommand { get; private set; }

        public IObservable<Unit> ConnectionFailed { get; private set; }

        public bool IsConnected
        {
            get { return this.isConnected.Value; }
        }

        private static async Task ConnectAsync()
        {
            IPAddress address = await NetworkMessenger.DiscoverServer();

            await NetworkMessenger.Instance.ConnectAsync(address);
        }
    }
}