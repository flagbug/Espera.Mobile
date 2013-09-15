using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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

            return CreateResponseInfo(response);
        }

        public async Task ConnectAsync(IPAddress address)
        {
            if (address == null)
                throw new ArgumentNullException("address");

            this.serverAddress = address;
            try
            {
                await this.client.ConnectAsync(this.serverAddress, Port);
            }
            catch (Exception ex)
            {
                Debugger.Break();
                throw;
            }
        }

        public void Dispose()
        {
            this.client.Close();
        }

        public async Task<Playlist> GetCurrentPlaylist()
        {
            JObject response = await this.SendRequest("get-current-playlist");

            JToken content = response["content"];

            string name = response["name"].ToString();

            List<Song> songs = response["songs"]
                .Select(x =>
                    new Song(String.Empty, x["title"].ToString(), String.Empty, String.Empty, TimeSpan.Zero, Guid.Parse(x["guid"].ToString())))
                .ToList();

            return new Playlist(name, songs);
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

        public async Task<Tuple<int, string>> PlaySongs(IEnumerable<Guid> guids)
        {
            var parameters = new JObject
            {
                {"guids", new JArray(guids.Select(x => x.ToString()).ToArray())}
            };

            JObject response = await this.SendRequest("post-play-instantly", parameters);

            return CreateResponseInfo(response);
        }

        private static Tuple<int, string> CreateResponseInfo(JObject response)
        {
            return Tuple.Create(response["status"].ToObject<int>(), response["message"].ToString());
        }

        private static async Task<byte[]> DecompressContentAsync(byte[] buffer)
        {
            using (var stream = new GZipStream(new MemoryStream(buffer), CompressionMode.Decompress))
            {
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);
                    return memoryStream.ToArray();
                }
            }
        }

        private async Task<byte[]> ReceiveAsync(int length)
        {
            var buffer = new byte[length];
            int received = 0;

            while (received < length)
            {
                int bytesRecieved = await this.client.GetStream().ReadAsync(buffer, received, buffer.Length - received);
                received += bytesRecieved;
            }

            return buffer;
        }

        private async Task<JObject> ReceiveMessage()
        {
            byte[] buffer = await this.ReceiveAsync(42);

            string header = Encoding.Unicode.GetString(buffer);

            if (header != "espera-server-message")
                throw new Exception("Holy batman, something went terribly wrong!");

            buffer = await this.ReceiveAsync(4);

            int length = BitConverter.ToInt32(buffer, 0);

            buffer = await this.ReceiveAsync(length);

            byte[] decompressed = await DecompressContentAsync(buffer);

            string content = Encoding.Unicode.GetString(decompressed);

            return JObject.Parse(content);
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