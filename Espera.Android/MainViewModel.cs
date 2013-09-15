using ReactiveUI;
using System.Net;
using System.Threading.Tasks;

namespace Espera.Android
{
    public class MainViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<bool> isConnected;

        public MainViewModel()
        {
            this.ConnectCommand = new ReactiveCommand();
            this.isConnected = this.ConnectCommand.RegisterAsyncTask(x => ConnectAsync())
                .ToProperty(this, x => x.IsConnected);
        }

        public ReactiveCommand ConnectCommand { get; private set; }

        public bool IsConnected
        {
            get { return this.isConnected.Value; }
        }

        private static async Task<bool> ConnectAsync()
        {
            IPAddress address = await NetworkMessenger.DiscoverServer();

            await NetworkMessenger.Instance.ConnectAsync(address);

            return true;
        }
    }
}