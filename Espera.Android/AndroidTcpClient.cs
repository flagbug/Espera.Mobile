using Espera.Mobile.Core.Network;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Espera.Android
{
    internal class AndroidTcpClient : ITcpClient
    {
        private readonly TcpClient client;

        public AndroidTcpClient()
        {
            this.client = new TcpClient();
        }

        public async Task ConnectAsync(string ipAddress, int port)
        {
            try
            {
                await this.client.ConnectAsync(ipAddress, port);
            }

            catch (SocketException ex)
            {
                throw new NetworkException("Failed to connect to the host.", ex);
            }
        }

        public void Dispose() => this.client.Close();

        public Stream GetStream() => this.client.GetStream();
    }
}