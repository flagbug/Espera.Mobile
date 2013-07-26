using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Exception = System.Exception;

namespace Espera.Android
{
    internal class NetworkMessenger : IDisposable
    {
        private static readonly int Port;
        private readonly TcpClient client;
        private readonly IPAddress serverAddress;

        static NetworkMessenger()
        {
            Port = 12345;
        }

        public NetworkMessenger(IPAddress serverAddress)
        {
            if (serverAddress == null)
                throw new ArgumentNullException("serverAddress");

            this.serverAddress = serverAddress;
            this.client = new TcpClient();
        }

        public static async Task<IPAddress> DiscoverServer()
        {
            var client = new UdpClient(Port);

            UdpReceiveResult result;
            do
            {
                result = await client.ReceiveAsync();
            }
            while (Encoding.Unicode.GetString(result.Buffer) != "espera-server-discovery");

            return result.RemoteEndPoint.Address;
        }

        public async Task ConnectAsync()
        {
            await this.client.ConnectAsync(this.serverAddress, Port);

            await this.GetSongs();
        }

        public void Dispose()
        {
            this.client.Close();
        }

        public async Task<IEnumerable<Song>> GetSongs()
        {
            string json = await this.Get("get-library-content");

            JToken array = JObject.Parse(json)["songs"];

            List<Song> songs = array
                .Select(s =>
                    new Song(s["artist"].ToString(), s["title"].ToString(), s["genre"].ToString(), s["album"].ToString()))
                .ToList();

            return songs;
        }

        private async Task<string> Get(string action)
        {
            byte[] message = Encoding.Unicode.GetBytes(action + "\n");
            this.client.Client.Send(message);

            var buffer = new byte[42];

            await this.RecieveAsync(buffer);

            string header = Encoding.Unicode.GetString(buffer);

            if (header != "espera-server-message")
                throw new Exception("Holy batman, something went terribly wrong!");

            buffer = new byte[4];
            await this.RecieveAsync(buffer);

            int length = BitConverter.ToInt32(buffer, 0);

            buffer = new byte[length];

            await this.RecieveAsync(buffer);

            string content = Encoding.Unicode.GetString(buffer);

            return content;
        }

        private async Task RecieveAsync(byte[] buffer)
        {
            int recieved = 0;

            while (recieved < buffer.Length)
            {
                recieved += await this.client.GetStream().ReadAsync(buffer, recieved, buffer.Length);
            }
        }
    }
}