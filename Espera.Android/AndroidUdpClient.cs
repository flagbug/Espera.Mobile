using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Espera.Mobile.Core.Network;

namespace Espera.Android
{
    internal class AndroidUdpClient : IUdpClient
    {
        private UdpClient client;

        public void Dispose()
        {
            this.client.Close();
        }

        public void Initialize(string ipAddress, int port)
        {
            this.client = new UdpClient(new IPEndPoint(IPAddress.Parse(ipAddress), port));
        }

        public async Task<Tuple<byte[], string>> ReceiveAsync()
        {
            UdpReceiveResult result;

            try
            {
                result = await this.client.ReceiveAsync();
            }

            // This happens when we dispose the UdpClient, but are still trying to receive a message
            catch (ObjectDisposedException)
            {
                return null;
            }

            return Tuple.Create(result.Buffer, result.RemoteEndPoint.Address.ToString());
        }
    }
}