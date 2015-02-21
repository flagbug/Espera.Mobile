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