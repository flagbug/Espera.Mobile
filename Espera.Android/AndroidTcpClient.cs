using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Espera.Mobile.Core.Network;

namespace Espera.Android
{
    internal class AndroidTcpClient : ITcpClient
    {
        private readonly TcpClient client;

        public AndroidTcpClient()
        {
            this.client = new TcpClient();
        }

        public Task ConnectAsync(string ipAddress, int port)
        {
            return this.client.ConnectAsync(ipAddress, port);
        }

        public void Dispose()
        {
            this.client.Close();
        }

        public Stream GetStream()
        {
            return this.client.GetStream();
        }
    }
}