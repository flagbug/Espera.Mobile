using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Espera.Mobile.Core.Network;
using Splat;

namespace Espera.Android
{
    internal class AndroidUdpClient : IUdpClient, IEnableLogger
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
                this.Log().Error("Encountered an ObjectDisposedException, but this is probably okay since " +
                                 "we disposed the UdpClient manually and the UDP receive was interrupted.");
                return null;
            }

            return Tuple.Create(result.Buffer, result.RemoteEndPoint.Address.ToString());
        }
    }
}