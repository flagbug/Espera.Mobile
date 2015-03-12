using Espera.Mobile.Core.Network;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Espera.WinPhone.Platform
{
    internal class WinPhoneUdpClient : IUdpClient
    {
        private DatagramSocket client;
        private int port;

        public void Dispose()
        {
            this.client.Dispose();
        }

        public void Initialize(string ipAddress, int port)
        {
            this.client = new DatagramSocket();
            this.port = port;
        }

        public async Task<Tuple<byte[], string>> ReceiveAsync()
        {
            var source = new TaskCompletionSource<Tuple<byte[], string>>();
            this.client.MessageReceived += (sender, args) =>
            {
                string remoteAddress = args.RemoteAddress.CanonicalName;

                DataReader reader = args.GetDataReader();

                byte[] data = reader.ReadBuffer(reader.UnconsumedBufferLength).ToArray();

                source.TrySetResult(Tuple.Create(data, remoteAddress));
            };

            await this.client.BindServiceNameAsync(this.port.ToString());

            return await source.Task;
        }
    }
}