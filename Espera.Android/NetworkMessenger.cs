using System.Reactive.Concurrency;
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
using System.Reactive;
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
        private readonly Subject<Unit> disconnected;
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
            this.disconnected = new Subject<Unit>();
        }

        public static NetworkMessenger Instance
        {
            get { return instance.Value; }
        }

        public bool Connected
        {
            get { return this.client.IsConnected; }
        }

        public IObservable<Unit> Disconnected { get; private set; }

        public IObservable<PlaybackState> PlaybackStateChanged { get; private set; }

        public IObservable<Playlist> PlaylistChanged { get; private set; }

        public IObservable<int?> PlaylistIndexChanged { get; private set; }

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

            this.Disconnected = Observable.FromEventPattern(h => this.client.Disconnected += h, h => this.client.Disconnected -= h)
                .Select(_ => Unit.Default)
                .Merge(this.disconnected)
                .FirstAsync();

            var conn = this.client.Receiver.Buffer(4)
                    .Select(length => BitConverter.ToInt32(length.ToArray(), 0))
                    .Select(length => this.client.Receiver.Take(length).ToEnumerable().ToArray())
                    .SelectMany(body => DecompressDataAsync(body).ToObservable())
                    .Select(body => Encoding.UTF8.GetString(body))
                    .Select(JObject.Parse)
                    .SubscribeOn(RxApp.TaskpoolScheduler)
                    .Publish();

            this.messagePipeline = conn;
            this.messagePipelineConnection = conn.Connect();

            var pushMessages = this.messagePipeline.Where(x => x["type"].ToString() == "push");

            this.PlaylistChanged = pushMessages.Where(x => x["action"].ToString() == "update-current-playlist")
                .Select(x => Playlist.Deserialize(x["content"]));

            this.PlaylistIndexChanged = pushMessages.Where(x => x["action"].ToString() == "update-current-index")
                .Select(x => x["content"]["index"].ToObject<int?>());

            this.PlaybackStateChanged = pushMessages.Where(x => x["action"].ToString() == "update-playback-state")
                .Select(x => x["content"]["state"].ToObject<PlaybackState>());

            await this.client.ConnectAsync();
        }

        public async Task<Tuple<int, string>> ContinueSong()
        {
            JObject response = await this.SendRequest("post-continue-song");

            return CreateResponseInfo(response);
        }

        public void Dispose()
        {
            if (this.client != null)
            {
                try
                {
                    this.client.Dispose();
                }

                catch (ObjectDisposedException)
                {
                    // Reactivesockets bug
                }
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

        public async Task<PlaybackState> GetPlaybackSate()
        {
            JObject response = await this.SendRequest("get-playback-state");

            JToken content = response["content"];

            return content["state"].ToObject<PlaybackState>();
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

        public async Task<Tuple<int, string>> PauseSong()
        {
            JObject response = await this.SendRequest("post-pause-song");

            return CreateResponseInfo(response);
        }

        public async Task<Tuple<int, string>> PlayNextSong()
        {
            JObject response = await this.SendRequest("post-play-next-song");

            return CreateResponseInfo(response);
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

        public async Task<Tuple<int, string>> PlayPreviousSong()
        {
            JObject response = await this.SendRequest("post-play-previous-song");

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
            byte[] contentBytes = Encoding.UTF8.GetBytes(content.ToString(Formatting.None));
            contentBytes = await CompressDataAsync(contentBytes);
            byte[] length = BitConverter.GetBytes(contentBytes.Length); // We have a fixed size of 4 bytes

            byte[] message = length.Concat(contentBytes).ToArray();

            await this.gate.WaitAsync();
            
            try
            {
                await client.SendAsync(message);
            }

            finally
            {
                this.gate.Release();
            }
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

            var message = this.messagePipeline.Where(x => x["type"].ToString() == "response")
                .FirstAsync(x => x["id"].ToString() == id.ToString())
                .PublishLast();

            using (message.Connect())
            {
                try
                {
                    await this.SendMessage(jMessage);
                }

                catch (Exception)
                {
                    this.disconnected.OnNext(Unit.Default);

                    return new JObject
                    {
                        {"status", 503},
                        {"message", "Connection lost"}
                    };
                }

                JObject response = await message;

                return response;
            }
        }
    }
}