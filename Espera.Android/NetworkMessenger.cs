using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveSockets;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Espera.Android
{
    internal class NetworkMessenger : IDisposable
    {
        private static readonly Lazy<NetworkMessenger> instance;
        private static readonly int Port;
        private readonly SemaphoreSlim gate;
        private ReactiveClient client;
        private IObservable<JObject> messagePipeline;
        private IDisposable messagePipelineConnection;

        static NetworkMessenger()
        {
            Port = 12345;
            instance = new Lazy<NetworkMessenger>(() => new NetworkMessenger());
        }

        private NetworkMessenger()
        {
            this.gate = new SemaphoreSlim(1, 1);
            this.messagePipeline = new Subject<JObject>();
        }

        public static NetworkMessenger Instance
        {
            get { return instance.Value; }
        }

        public bool Connected
        {
            get { return this.client.IsConnected; }
        }

        public IObservable<Playlist> PlaylistChanged { get; private set; }

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

        public async Task<Tuple<int, string>> AddSongToPlaylist(Guid songGuid)
        {
            var parameters = new JObject
            {
                { "songGuid", songGuid.ToString() }
            };

            JObject response = await this.SendRequest("post-playlist-song", parameters);

            return CreateResponseInfo(response);
        }

        public async Task ConnectAsync(IPAddress address)
        {
            if (address == null)
                throw new ArgumentNullException("address");

            this.client = new ReactiveClient(address.ToString(), Port);

            var conn = this.client.Receiver.Buffer(4)
                    .Select(length => BitConverter.ToInt32(length.ToArray(), 0))
                    .Select(length => this.client.Receiver.Take(length).ToEnumerable().ToArray())
                    .SelectMany(body => DecompressDataAsync(body).ToObservable())
                    .Select(body => Encoding.Unicode.GetString(body))
                    .Select(JObject.Parse)
                    .SubscribeOn(RxApp.TaskpoolScheduler)
                    .Publish();

            this.messagePipeline = conn;
            this.messagePipelineConnection = conn.Connect();

            this.PlaylistChanged = this.messagePipeline.Where(x => x["type"].ToString() == "push")
                .Select(x => Playlist.Deserialize(x["content"]));

            await this.client.ConnectAsync();
        }

        public void Dispose()
        {
            if (this.client != null)
            {
                this.client.Dispose();
            }

            if (this.messagePipelineConnection != null)
            {
                this.messagePipelineConnection.Dispose();
            }
        }

        public async Task<Playlist> GetCurrentPlaylist()
        {
            JObject response = await this.SendRequest("get-current-playlist");

            JToken content = response["content"];

            return Playlist.Deserialize(content);
        }

        public async Task<IReadOnlyList<Song>> GetSongsAsync()
        {
            JObject response = await this.SendRequest("get-library-content");

            List<Song> songs = response["content"]["songs"]
                .Select(s =>
                    new Song(s["artist"].ToString(), s["title"].ToString(), s["genre"].ToString(),
                        s["album"].ToString(), TimeSpan.FromSeconds(s["duration"].ToObject<double>()),
                        Guid.Parse(s["guid"].ToString()), SongSource.Local))
                .ToList();

            return songs;
        }

        public async Task<Tuple<int, string>> PlayPlaylistSong(Guid guid)
        {
            var parameters = new JObject
            {
                { "entryGuid", guid.ToString() }
            };

            JObject response = await this.SendRequest("post-play-playlist-song", parameters);

            return CreateResponseInfo(response);
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

        private static async Task<byte[]> CompressDataAsync(byte[] data)
        {
            using (var targetStream = new MemoryStream())
            {
                using (var stream = new GZipStream(targetStream, CompressionMode.Compress))
                {
                    await stream.WriteAsync(data, 0, data.Length);
                }

                return targetStream.ToArray();
            }
        }

        private static Tuple<int, string> CreateResponseInfo(JObject response)
        {
            return Tuple.Create(response["status"].ToObject<int>(), response["message"].ToString());
        }

        private static async Task<byte[]> DecompressDataAsync(byte[] buffer)
        {
            using (var sourceStream = new MemoryStream(buffer))
            {
                using (var stream = new GZipStream(sourceStream, CompressionMode.Decompress))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);
                        return memoryStream.ToArray();
                    }
                }
            }
        }

        private async Task SendMessage(JObject content)
        {
            byte[] contentBytes = Encoding.Unicode.GetBytes(content.ToString(Formatting.None));
            contentBytes = await CompressDataAsync(contentBytes);
            byte[] length = BitConverter.GetBytes(contentBytes.Length); // We have a fixed size of 4 bytes

            byte[] message = length.Concat(contentBytes).ToArray();

            await this.gate.WaitAsync();
            await client.SendAsync(message);
            this.gate.Release();
        }

        private async Task<JObject> SendRequest(string action, JToken parameters = null)
        {
            Guid id = Guid.NewGuid();

            var jMessage = new JObject
            {
                { "action", action },
                { "parameters", parameters },
                { "id", id.ToString()}
            };

            var responseSubject = new AsyncSubject<JObject>();

            this.messagePipeline.FirstAsync(x => x["id"].ToString() == id.ToString())
                .Subscribe(x =>
                {
                    responseSubject.OnNext(x);
                    responseSubject.OnCompleted();
                });

            await this.SendMessage(jMessage);

            JObject response = await responseSubject;

            return response;
        }
    }
}