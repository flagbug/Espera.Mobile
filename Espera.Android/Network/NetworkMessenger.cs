using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReactiveSockets;
using ReactiveUI;
using System;
using System.Collections.Generic;
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

namespace Espera.Android.Network
{
    public class NetworkMessenger : IDisposable, INetworkMessenger
    {
        private static IPAddress fakeIpAddress; // Used for unit tests
        private static Lazy<INetworkMessenger> instance;
        private readonly Subject<AccessPermission> accessPermission;
        private readonly Subject<ReactiveClient> client;
        private readonly Subject<Unit> connectionEstablished;
        private readonly Subject<Unit> disconnected;
        private readonly SemaphoreSlim gate;
        private readonly IObservable<JObject> messagePipeline;
        private readonly IDisposable messagePipelineConnection;
        private ReactiveClient currentClient;

        static NetworkMessenger()
        {
            instance = new Lazy<INetworkMessenger>(() => new NetworkMessenger());
        }

        private NetworkMessenger()
        {
            this.gate = new SemaphoreSlim(1, 1);
            this.messagePipeline = new Subject<JObject>();
            this.disconnected = new Subject<Unit>();
            this.connectionEstablished = new Subject<Unit>();
            this.accessPermission = new Subject<AccessPermission>();

            this.client = new Subject<ReactiveClient>();

            this.Disconnected = this.client.Select(x => Observable.FromEventPattern(h => x.Disconnected += h, h => x.Disconnected -= h))
                .Switch().Select(_ => Unit.Default)
                .Merge(this.disconnected);

            var isConnected = this.Disconnected.Select(_ => false)
                .Merge(this.connectionEstablished.Select(_ => true))
                .DistinctUntilChanged()
                .Publish(false);
            isConnected.Connect();
            this.IsConnected = isConnected;

            var conn = this.client.Select(x => x.Receiver.Buffer(4)
                    .Select(length => BitConverter.ToInt32(length.ToArray(), 0))
                    .Select(length => x.Receiver.Take(length).ToEnumerable().ToArray())
                    .SelectMany(body => NetworkHelpers.DecompressDataAsync(body).ToObservable())
                    .Select(body => Encoding.UTF8.GetString(body))
                    .Select(JObject.Parse))
                .Switch()
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

            var accessPermissionConn = pushMessages.Where(x => x["action"].ToString() == "update-access-permission")
                .Select(x => x["content"]["accessPermission"].ToObject<AccessPermission>())
                .Merge(this.accessPermission)
                .Publish(Android.AccessPermission.Guest);
            accessPermissionConn.Connect();

            this.AccessPermission = accessPermissionConn;
        }

        public static INetworkMessenger Instance
        {
            get { return instance.Value; }
        }

        public IObservable<AccessPermission> AccessPermission { get; private set; }

        public IObservable<Unit> Disconnected { get; private set; }

        public IObservable<bool> IsConnected { get; private set; }

        public IObservable<PlaybackState> PlaybackStateChanged { get; private set; }

        public IObservable<Playlist> PlaylistChanged { get; private set; }

        public IObservable<int?> PlaylistIndexChanged { get; private set; }

        public static async Task<IPAddress> DiscoverServer(int port)
        {
            if (fakeIpAddress != null)
                return fakeIpAddress;

            var client = new UdpClient(port);

            UdpReceiveResult result;

            do
            {
                result = await client.ReceiveAsync();
            }
            while (Encoding.Unicode.GetString(result.Buffer) != "espera-server-discovery");

            return result.RemoteEndPoint.Address;
        }

        /// <summary>
        /// Override the messenger instance for unit testing.
        /// </summary>
        /// <param name="messenger">The messenger mock.</param>
        /// <param name="ipAdress">An optional IpAdress to return for the <see cref="DiscoverServer"/> function.</param>
        public static void Override(INetworkMessenger messenger, IPAddress ipAdress = null)
        {
            if (messenger == null)
                throw new ArgumentNullException("messenger");

            instance = new Lazy<INetworkMessenger>(() => messenger);
            fakeIpAddress = ipAdress;
        }

        public async Task<ResponseInfo> AddSongToPlaylist(Guid songGuid)
        {
            var parameters = new JObject
            {
                { "songGuid", songGuid.ToString() }
            };

            JObject response = await this.SendRequest("post-playlist-song", parameters);

            return CreateResponseInfo(response);
        }

        public async Task<ResponseInfo> Authorize(string password)
        {
            var parameters = new JObject
            {
                {"password", password}
            };

            JObject response = await this.SendRequest("post-administrator-password", parameters);

            return CreateResponseInfo(response);
        }

        public async Task ConnectAsync(IPAddress address, int port)
        {
            if (address == null)
                throw new ArgumentNullException("address");

            if (this.currentClient != null)
            {
                this.currentClient.Dispose();
            }

            var c = new ReactiveClient(address.ToString(), port);
            this.currentClient = c;
            this.client.OnNext(c);

            await c.ConnectAsync();

            this.connectionEstablished.OnNext(Unit.Default);

            JObject response = await this.SendRequest("get-access-permission");

            var permission = response["content"]["accessPermission"].ToObject<AccessPermission>();

            this.accessPermission.OnNext(permission);
        }

        public async Task<ResponseInfo> ContinueSong()
        {
            JObject response = await this.SendRequest("post-continue-song");

            return CreateResponseInfo(response);
        }

        public void Disconnect()
        {
            this.currentClient.Disconnect();
        }

        public void Dispose()
        {
            if (this.currentClient != null)
            {
                this.currentClient.Dispose();
            }

            if (this.messagePipelineConnection != null)
            {
                this.messagePipelineConnection.Dispose();
            }
        }

        public async Task<AccessPermission> GetAccessPermission()
        {
            JObject response = await this.SendRequest("get-access-permission");

            JToken content = response["content"];

            return content["accessPermission"].ToObject<AccessPermission>();
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

        public async Task<Version> GetServerVersion()
        {
            JObject response = await this.SendRequest("get-server-version");

            JToken content = response["content"];

            var version = new Version(content["version"].ToString());

            return version;
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

        public async Task<ResponseInfo> PauseSong()
        {
            JObject response = await this.SendRequest("post-pause-song");

            return CreateResponseInfo(response);
        }

        public async Task<ResponseInfo> PlayNextSong()
        {
            JObject response = await this.SendRequest("post-play-next-song");

            return CreateResponseInfo(response);
        }

        public async Task<ResponseInfo> PlayPlaylistSong(Guid guid)
        {
            var parameters = new JObject
            {
                { "entryGuid", guid.ToString() }
            };

            JObject response = await this.SendRequest("post-play-playlist-song", parameters);

            return CreateResponseInfo(response);
        }

        public async Task<ResponseInfo> PlayPreviousSong()
        {
            JObject response = await this.SendRequest("post-play-previous-song");

            return CreateResponseInfo(response);
        }

        public async Task<ResponseInfo> PlaySongs(IEnumerable<Guid> guids)
        {
            var parameters = new JObject
            {
                {"guids", new JArray(guids.Select(x => x.ToString()).ToArray())}
            };

            JObject response = await this.SendRequest("post-play-instantly", parameters);

            return CreateResponseInfo(response);
        }

        public async Task<ResponseInfo> RemovePlaylistSong(Guid guid)
        {
            var parameters = new JObject
            {
                { "entryGuid", guid.ToString() }
            };

            JObject response = await this.SendRequest("post-remove-playlist-song", parameters);

            return CreateResponseInfo(response);
        }

        private static ResponseInfo CreateResponseInfo(JObject response)
        {
            return new ResponseInfo(response["status"].ToObject<int>(), response["message"].ToString());
        }

        private async Task SendMessage(JObject content)
        {
            byte[] contentBytes = Encoding.UTF8.GetBytes(content.ToString(Formatting.None));
            contentBytes = await NetworkHelpers.CompressDataAsync(contentBytes);
            byte[] length = BitConverter.GetBytes(contentBytes.Length); // We have a fixed size of 4 bytes

            byte[] message = length.Concat(contentBytes).ToArray();

            await this.gate.WaitAsync();

            try
            {
                await this.currentClient.SendAsync(message);
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