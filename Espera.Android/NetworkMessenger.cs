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
        private static readonly Lazy<NetworkMessenger> instance;
        private static readonly int Port;
        private readonly TcpClient client;
        private IPAddress serverAddress;

        static NetworkMessenger()
        {
            Port = 12345;
            instance = new Lazy<NetworkMessenger>(() => new NetworkMessenger());
        }

        public NetworkMessenger()
        {
            this.client = new TcpClient();
        }

        public static NetworkMessenger Instance
        {
            get { return instance.Value; }
        }

        public bool Connected
        {
            get { return this.client.Connected; }
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

        public async Task<Tuple<int, string>> AddSongToPlaylist(Song song)
        {
            var parameters = new JObject
            {
                { "songGuid", song.Guid.ToString() }
            };

            JObject response = await this.SendRequest("post-playlist-song", parameters);

            return Tuple.Create(response["status"].ToObject<int>(), response["message"].ToString());
        }

        public async Task ConnectAsync(IPAddress address)
        {
            if (address == null)
                throw new ArgumentNullException("address");

            this.serverAddress = address;
            await this.client.ConnectAsync(this.serverAddress, Port);
        }

        public void Dispose()
        {
            this.client.Close();
        }

        public async Task<IReadOnlyList<Song>> GetSongsAsync()
        {
            JObject response = await this.SendRequest("get-library-content");

            List<Song> songs = response["content"]["songs"]
                .Select(s =>
                    new Song(s["artist"].ToString(), s["title"].ToString(), s["genre"].ToString(),
                        s["album"].ToString(), TimeSpan.FromSeconds(s["duration"].ToObject<double>()),
                        Guid.Parse(s["guid"].ToString())))
                .ToList();

            return songs;
        }

        private async Task<JObject> ReceiveMessage()
        {
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

            return JObject.Parse(content);
        }

        private async Task RecieveAsync(byte[] buffer)
        {
            int recieved = 0;

            while (recieved < buffer.Length)
            {
                int bytesRecieved = await this.client.GetStream().ReadAsync(buffer, recieved, buffer.Length - recieved);
                recieved += bytesRecieved;
            }
        }

        private async Task SendMessage(JObject content)
        {
            byte[] contentBytes = Encoding.Unicode.GetBytes(content.ToString());
            byte[] length = BitConverter.GetBytes(contentBytes.Length); // We have a fixed size of 4 bytes
            byte[] headerBytes = Encoding.Unicode.GetBytes("espera-client-message");

            var message = new byte[headerBytes.Length + length.Length + contentBytes.Length];
            headerBytes.CopyTo(message, 0);
            length.CopyTo(message, headerBytes.Length);
            contentBytes.CopyTo(message, headerBytes.Length + length.Length);

            await client.GetStream().WriteAsync(message, 0, message.Length);
            await client.GetStream().FlushAsync();
        }

        private async Task<JObject> SendRequest(string action, JToken parameters = null)
        {
            var jMessage = new JObject
            {
                { "action", action },
                { "parameters", parameters }
            };

            await this.SendMessage(jMessage);

            return await this.ReceiveMessage();
        }
    }
}