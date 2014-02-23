using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        private readonly Subject<TcpClient> client;
        private readonly Subject<Unit> connectionEstablished;
        private readonly Subject<Unit> disconnected;
        private readonly SemaphoreSlim gate;
        private readonly IObservable<JObject> messagePipeline;
        private readonly IDisposable messagePipelineConnection;
        private TcpClient currentClient;

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

            this.client = new Subject<TcpClient>();

            var isConnected = this.Disconnected.Select(_ => false)
                .Merge(this.connectionEstablished.Select(_ => true))
                .DistinctUntilChanged()
                .Publish(false);
            isConnected.Connect();
            this.IsConnected = isConnected;

            var pipeline = this.client.Select(x => Observable.Defer(() => x.ReadNextMessageAsync()
                    .ToObservable())
                    .Repeat()
                    .TakeWhile(m => m != null)
                    .Finally(() => this.disconnected.OnNext(Unit.Default))
                    .Catch(Observable.Never<JObject>()))
                .Switch()
                .Publish();
            // Serialize all incoming messages to the main thread scheduler,
            // as we process them on the UI anyway
            this.messagePipeline = pipeline.ObserveOn(RxApp.MainThreadScheduler);
            this.messagePipelineConnection = pipeline.Connect();

            var pushMessages = this.messagePipeline.Where(x => x["type"].ToString() == "push");

            this.PlaylistChanged = pushMessages.Where(x => x["action"].ToString() == "update-current-playlist")
                .Select(x => Playlist.Deserialize(x["content"]));

            this.PlaybackStateChanged = pushMessages.Where(x => x["action"].ToString() == "update-playback-state")
                .Select(x => x["content"]["state"].ToObject<PlaybackState>());

            this.RemainingVotesChanged = pushMessages
                .Where(x => x["action"].ToString() == "update-remaining-votes")
                .Select(x => x["content"]["remainingVotes"].ToObject<int?>());

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

        public IObservable<Unit> Disconnected
        {
            get { return this.disconnected.AsObservable(); }
        }

        public IObservable<bool> IsConnected { get; private set; }

        public IObservable<PlaybackState> PlaybackStateChanged { get; private set; }

        public IObservable<Playlist> PlaylistChanged { get; private set; }

        public IObservable<int?> RemainingVotesChanged { get; private set; }

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

        public async Task<ResponseInfo> AddSongToPlaylistAsync(Guid songGuid)
        {
            var parameters = JObject.FromObject(new
            {
                songGuid
            });

            JObject response = await this.SendRequest("post-playlist-song", parameters);

            return CreateResponseInfo(response);
        }

        /// <summary>
        /// Connects to the server with an optional password that requests administrator rights.
        /// </summary>
        /// <param name="address">The server's IP address.</param>
        /// <param name="port">The server's port.</param>
        /// <param name="deviceId">A, to this device unique identifier.</param>
        /// <param name="password">The optional administrator password. <c>null</c>, if guest rights are requested.</param>
        public async Task<ConnectionInfo> ConnectAsync(IPAddress address, int port, Guid deviceId, string password)
        {
            if (address == null)
                throw new ArgumentNullException("address");

            if (this.currentClient != null)
            {
                this.currentClient.Close();
            }

            var c = new TcpClient();
            this.currentClient = c;

            await c.ConnectAsync(address, port);
            this.client.OnNext(c);

            var parameters = JObject.FromObject(new
            {
                deviceId,
                password
            });

            JObject response = await this.SendRequest("get-connection-info", parameters);

            ResponseInfo responseInfo = CreateResponseInfo(response);
            var permission = response["content"]["accessPermission"].ToObject<AccessPermission>();
            var serverVersion = response["content"]["serverVersion"].ToObject<Version>();

            var connectionInfo = new ConnectionInfo(permission, serverVersion, responseInfo);

            if (responseInfo.StatusCode == 200)
            {
                this.connectionEstablished.OnNext(Unit.Default);
                this.accessPermission.OnNext(permission);
            }

            return connectionInfo;
        }

        public async Task<ResponseInfo> ContinueSongAsync()
        {
            JObject response = await this.SendRequest("post-continue-song");

            return CreateResponseInfo(response);
        }

        public void Disconnect()
        {
            this.currentClient.Close();
            this.currentClient = null;
        }

        public void Dispose()
        {
            if (this.currentClient != null)
            {
                this.currentClient.Close();
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

        public async Task<Playlist> GetCurrentPlaylistAsync()
        {
            JObject response = await this.SendRequest("get-current-playlist");

            JToken content = response["content"];

            return Playlist.Deserialize(content);
        }

        public async Task<PlaybackState> GetPlaybackStateAsync()
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

        public async Task<ResponseInfo> MovePlaylistSongDownAsync(Guid entryGuid)
        {
            var parameters = JObject.FromObject(new
            {
                entryGuid
            });

            JObject response = await this.SendRequest("move-playlist-song-down", parameters);

            return CreateResponseInfo(response);
        }

        public async Task<ResponseInfo> MovePlaylistSongUpAsync(Guid entryGuid)
        {
            var parameters = JObject.FromObject(new
            {
                entryGuid
            });

            JObject response = await this.SendRequest("move-playlist-song-up", parameters);

            return CreateResponseInfo(response);
        }

        public async Task<ResponseInfo> PauseSongAsync()
        {
            JObject response = await this.SendRequest("post-pause-song");

            return CreateResponseInfo(response);
        }

        public async Task<ResponseInfo> PlayNextSongAsync()
        {
            JObject response = await this.SendRequest("post-play-next-song");

            return CreateResponseInfo(response);
        }

        public async Task<ResponseInfo> PlayPlaylistSongAsync(Guid entryGuid)
        {
            var parameters = JObject.FromObject(new
            {
                entryGuid
            });

            JObject response = await this.SendRequest("post-play-playlist-song", parameters);

            return CreateResponseInfo(response);
        }

        public async Task<ResponseInfo> PlayPreviousSongAsync()
        {
            JObject response = await this.SendRequest("post-play-previous-song");

            return CreateResponseInfo(response);
        }

        public async Task<ResponseInfo> PlaySongsAsync(IEnumerable<Guid> guids)
        {
            var parameters = JObject.FromObject(new
            {
                guids = guids.Select(x => x.ToString())
            });

            JObject response = await this.SendRequest("post-play-instantly", parameters);

            return CreateResponseInfo(response);
        }

        public async Task<ResponseInfo> RemovePlaylistSongAsync(Guid entryGuid)
        {
            var parameters = JObject.FromObject(new
            {
                entryGuid
            });

            JObject response = await this.SendRequest("post-remove-playlist-song", parameters);

            return CreateResponseInfo(response);
        }

        public async Task<ResponseInfo> VoteAsync(Guid entryGuid)
        {
            var parameters = JObject.FromObject(new
            {
                entryGuid
            });

            JObject response = await this.SendRequest("vote-for-song", parameters);

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
                await this.currentClient.GetStream().WriteAsync(message, 0, message.Length);
            }

            finally
            {
                this.gate.Release();
            }
        }

        private async Task<JObject> SendRequest(string action, JToken parameters = null)
        {
            Guid id = Guid.NewGuid();

            var jMessage = JObject.FromObject(new
            {
                action,
                parameters,
                id
            });

            var message = this.messagePipeline
                .FirstAsync(x => x["type"].ToString() == "response" && x["id"].ToString() == id.ToString())
                .PublishLast();

            using (message.Connect())
            {
                try
                {
                    await this.SendMessage(jMessage);
                }

                catch (Exception ex)
                {
                    this.disconnected.OnNext(Unit.Default);

                    return JObject.FromObject(new
                    {
                        status = 503,
                        message = "Connection lost"
                    });
                }

                JObject response = await message;

                return response;
            }
        }
    }
}