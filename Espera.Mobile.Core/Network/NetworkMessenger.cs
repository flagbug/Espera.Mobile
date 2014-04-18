using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using Espera.Mobile.Core.Analytics;
using Espera.Network;
using Newtonsoft.Json.Linq;
using ReactiveUI;

namespace Espera.Mobile.Core.Network
{
    public class NetworkMessenger : IDisposable, INetworkMessenger
    {
        private static IPAddress fakeIpAddress; // Used for unit tests
        private static Lazy<INetworkMessenger> instance;
        private readonly Subject<NetworkAccessPermission> accessPermission;
        private readonly Subject<TcpClient> client;
        private readonly Subject<Unit> connectionEstablished;
        private readonly Subject<Unit> disconnected;
        private readonly SemaphoreSlim gate;
        private readonly IObservable<NetworkMessage> messagePipeline;
        private readonly IDisposable messagePipelineConnection;
        private IAnalytics analytics;
        private TcpClient currentClient;
        private TcpClient currentFileTransferClient;
        private SemaphoreSlim fileTransferGate;

        static NetworkMessenger()
        {
            instance = new Lazy<INetworkMessenger>(() => new NetworkMessenger());
        }

        private NetworkMessenger()
        {
            this.gate = new SemaphoreSlim(1, 1);
            this.fileTransferGate = new SemaphoreSlim(1, 1);
            this.messagePipeline = new Subject<NetworkMessage>();
            this.disconnected = new Subject<Unit>();
            this.connectionEstablished = new Subject<Unit>();
            this.accessPermission = new Subject<NetworkAccessPermission>();

            this.client = new Subject<TcpClient>();

            var isConnected = this.Disconnected.Select(_ => false)
                .Merge(this.connectionEstablished.Select(_ => true))
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Publish(false);
            isConnected.Connect();
            this.IsConnected = isConnected;

            var pipeline = this.client.Select(x => Observable.Defer(() => x.GetStream().ReadNextMessageAsync()
                    .ToObservable())
                    .Repeat()
                    .TakeWhile(m => m != null)
                    .Finally(() => this.disconnected.OnNext(Unit.Default))
                    .Catch(Observable.Never<NetworkMessage>()))
                .Switch()
                .Publish();
            // Serialize all incoming messages to the main thread scheduler, as we process them on
            // the UI anyway
            this.messagePipeline = pipeline.ObserveOn(RxApp.MainThreadScheduler);
            this.messagePipelineConnection = pipeline.Connect();

            var pushMessages = this.messagePipeline.Where(x => x.MessageType == NetworkMessageType.Push)
                .Select(x => x.Payload.ToObject<PushInfo>());

            this.PlaylistChanged = pushMessages.Where(x => x.PushAction == "update-current-playlist")
                .Select(x => x.Content.ToObject<NetworkPlaylist>());

            this.PlaybackStateChanged = pushMessages.Where(x => x.PushAction == "update-playback-state")
                .Select(x => x.Content["state"].ToObject<NetworkPlaybackState>());

            this.RemainingVotesChanged = pushMessages.Where(x => x.PushAction == "update-remaining-votes")
                .Select(x => x.Content["remainingVotes"].ToObject<int?>());

            var accessPermissionConn = pushMessages.Where(x => x.PushAction == "update-access-permission")
                .Select(x => x.Content["accessPermission"].ToObject<NetworkAccessPermission>())
                .Merge(this.accessPermission)
                .Publish(NetworkAccessPermission.Guest);
            accessPermissionConn.Connect();

            this.AccessPermission = accessPermissionConn;
        }

        public static INetworkMessenger Instance
        {
            get { return instance.Value; }
        }

        public IObservable<NetworkAccessPermission> AccessPermission { get; private set; }

        public IObservable<Unit> Disconnected
        {
            get { return this.disconnected.AsObservable(); }
        }

        public IObservable<bool> IsConnected { get; private set; }

        public IObservable<NetworkPlaybackState> PlaybackStateChanged { get; private set; }

        public IObservable<NetworkPlaylist> PlaylistChanged { get; private set; }

        public IObservable<int?> RemainingVotesChanged { get; private set; }

        public static Task<IPAddress> DiscoverServerAsync(IPAddress localAddress, int port)
        {
            if (localAddress == null && fakeIpAddress == null)
                throw new ArgumentNullException("localAddress");

            if (fakeIpAddress != null)
                return Task.FromResult(fakeIpAddress);

            return Observable.Using(() => new UdpClient(new IPEndPoint(localAddress, port)), x => Observable.FromAsync(x.ReceiveAsync))
                .Repeat()
                .FirstAsync(x => Encoding.Unicode.GetString(x.Buffer) == "espera-server-discovery")
                .Select(x => x.RemoteEndPoint.Address)
                .ToTask();
        }

        /// <summary>
        /// Override the messenger instance for unit testing.
        /// </summary>
        /// <param name="messenger">The messenger mock.</param>
        /// <param name="ipAdress">
        /// An optional <see cref="IPAddress" /> to return for the <see cref="DiscoverServerAsync"
        /// /> function.
        /// </param>
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

            ResponseInfo response = await this.SendRequest("post-playlist-song", parameters);

            return response;
        }

        /// <summary>
        /// Connects to the server with an optional password that requests administrator rights.
        /// </summary>
        /// <param name="address">The server's IP address.</param>
        /// <param name="port">The server's port.</param>
        /// <param name="deviceId">A, to this device unique identifier.</param>
        /// <param name="password">
        /// The optional administrator password. <c>null</c>, if guest rights are requested.
        /// </param>
        public async Task<Tuple<ResponseStatus, ConnectionInfo>> ConnectAsync(IPAddress address, int port, Guid deviceId, string password)
        {
            if (address == null)
                throw new ArgumentNullException("address");

            if (this.currentClient != null)
            {
                this.currentClient.Close();
            }

            if (this.currentFileTransferClient != null)
            {
                this.currentFileTransferClient.Close();
            }

            var c = new TcpClient();
            var f = new TcpClient();
            this.currentClient = c;
            this.currentFileTransferClient = f;

            await c.ConnectAsync(address, port);
            await f.ConnectAsync(address, port + 1);
            this.client.OnNext(c);

            var parameters = JObject.FromObject(new
            {
                deviceId,
                password
            });

            ResponseInfo response = await this.SendRequest("get-connection-info", parameters);

            var connectionInfo = response.Content.ToObject<ConnectionInfo>();

            if (response.Status == ResponseStatus.Success)
            {
                this.connectionEstablished.OnNext(Unit.Default);
                this.accessPermission.OnNext(connectionInfo.AccessPermission);
            }

            return Tuple.Create(response.Status, connectionInfo);
        }

        public async Task<ResponseInfo> ContinueSongAsync()
        {
            ResponseInfo response = await this.SendRequest("post-continue-song");

            return response;
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

        public async Task<NetworkAccessPermission> GetAccessPermission()
        {
            ResponseInfo response = await this.SendRequest("get-access-permission");

            return response.Content["accessPermission"].ToObject<NetworkAccessPermission>();
        }

        public async Task<NetworkPlaylist> GetCurrentPlaylistAsync()
        {
            ResponseInfo response = await this.SendRequest("get-current-playlist");

            return response.Content.ToObject<NetworkPlaylist>();
        }

        public async Task<NetworkPlaybackState> GetPlaybackStateAsync()
        {
            ResponseInfo response = await this.SendRequest("get-playback-state");

            return response.Content["state"].ToObject<NetworkPlaybackState>();
        }

        public async Task<IReadOnlyList<NetworkSong>> GetSongsAsync()
        {
            ResponseInfo response = await this.SendRequest("get-library-content");

            // This can take about a second, if there are many songs, so deserialize the songs on a
            // background thread
            var songs = await Task.Run(() => response.Content["songs"].ToObject<List<NetworkSong>>());

            return songs;
        }

        public async Task<ResponseInfo> MovePlaylistSongDownAsync(Guid entryGuid)
        {
            var parameters = JObject.FromObject(new
            {
                entryGuid
            });

            ResponseInfo response = await this.SendRequest("move-playlist-song-down", parameters);

            return response;
        }

        public async Task<ResponseInfo> MovePlaylistSongUpAsync(Guid entryGuid)
        {
            var parameters = JObject.FromObject(new
            {
                entryGuid
            });

            ResponseInfo response = await this.SendRequest("move-playlist-song-up", parameters);

            return response;
        }

        public async Task<ResponseInfo> PauseSongAsync()
        {
            ResponseInfo response = await this.SendRequest("post-pause-song");

            return response;
        }

        public async Task<ResponseInfo> PlayNextSongAsync()
        {
            ResponseInfo response = await this.SendRequest("post-play-next-song");

            return response;
        }

        public async Task<ResponseInfo> PlayPlaylistSongAsync(Guid entryGuid)
        {
            var parameters = JObject.FromObject(new
            {
                entryGuid
            });

            ResponseInfo response = await this.SendRequest("post-play-playlist-song", parameters);

            return response;
        }

        public async Task<ResponseInfo> PlayPreviousSongAsync()
        {
            ResponseInfo response = await this.SendRequest("post-play-previous-song");

            return response;
        }

        public async Task<ResponseInfo> PlaySongsAsync(IEnumerable<Guid> guids)
        {
            var parameters = JObject.FromObject(new
            {
                guids = guids.Select(x => x.ToString())
            });

            ResponseInfo response = await this.SendRequest("post-play-instantly", parameters);

            return response;
        }

        public async Task<FileTransferStatus> QueueRemoteSong(byte[] songData)
        {
            Guid transferId = Guid.NewGuid();
            var info = new FileTransferInfo { TransferId = transferId };

            ResponseInfo response = await this.SendRequest("queue-remote-song", JObject.FromObject(info));

            var message = new FileTransferMessage { Data = songData, TransferId = transferId };

            byte[] packed = await NetworkHelpers.PackFileTransferMessageAsync(message);

            IObservable<int> progress = this.TransferFileAsync(packed).Publish(0).PermaRef();

            var status = new FileTransferStatus(progress);

            return status;
        }

        /// <summary>
        /// Registers an analytics provider ton measure network timings
        /// </summary>
        /// <param name="analytics"></param>
        public void RegisterAnalytics(IAnalytics analytics)
        {
            if (analytics == null)
                throw new ArgumentNullException("analytics");

            this.analytics = analytics;
        }

        public async Task<ResponseInfo> RemovePlaylistSongAsync(Guid entryGuid)
        {
            var parameters = JObject.FromObject(new
            {
                entryGuid
            });

            ResponseInfo response = await this.SendRequest("post-remove-playlist-song", parameters);

            return response;
        }

        public async Task<ResponseInfo> VoteAsync(Guid entryGuid)
        {
            var parameters = JObject.FromObject(new
            {
                entryGuid
            });

            ResponseInfo response = await this.SendRequest("vote-for-song", parameters);

            return response;
        }

        private async Task SendMessage(NetworkMessage message)
        {
            byte[] packedMessage = await NetworkHelpers.PackMessageAsync(message);

            await this.gate.WaitAsync();

            try
            {
                await this.currentClient.GetStream().WriteAsync(packedMessage, 0, packedMessage.Length);
            }

            finally
            {
                this.gate.Release();
            }
        }

        private async Task<ResponseInfo> SendRequest(string action, JObject parameters = null)
        {
            Guid id = Guid.NewGuid();

            var requestInfo = new RequestInfo
            {
                RequestAction = action,
                Parameters = parameters,
                RequestId = id
            };

            var message = new NetworkMessage
            {
                MessageType = NetworkMessageType.Request,
                Payload = JObject.FromObject(requestInfo)
            };

            var responseMessage = this.messagePipeline
                .Where(x => x.MessageType == NetworkMessageType.Response)
                .Select(x => x.Payload.ToObject<ResponseInfo>())
                .FirstAsync(x => x.RequestId == id)
                .ToTask();

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                await this.SendMessage(message);

                ResponseInfo response = await responseMessage;

                stopwatch.Stop();

                if (analytics != null)
                {
                    this.analytics.RecordNetworkTiming(action, stopwatch.ElapsedMilliseconds);
                }

                return response;
            }

            catch (Exception ex)
            {
                stopwatch.Stop();

                this.disconnected.OnNext(Unit.Default);

                return new ResponseInfo
                {
                    Status = ResponseStatus.Fatal,
                    Message = "Connection lost",
                    RequestId = id
                };
            }
        }

        private IObservable<int> TransferFileAsync(byte[] data)
        {
            const int bufferSize = 32 * 1024;
            int written = 0;
            Stream stream = this.currentFileTransferClient.GetStream();

            byte[] length = BitConverter.GetBytes(data.Length); // We have a fixed size of 4 bytes

            var progress = new BehaviorSubject<int>(0);

            Task.Run(() =>
            {
                stream.Write(length, 0, length.Length);

                using (var dataStream = new MemoryStream(data))
                {
                    var buffer = new byte[bufferSize];
                    int count;

                    while ((count = dataStream.Read(buffer, 0, bufferSize)) > 0)
                    {
                        stream.Write(buffer, 0, count);
                        written += count;

                        progress.OnNext((int)(100 * ((double)written / data.Length)));
                    }
                }

                progress.OnCompleted();
            });

            return progress.DistinctUntilChanged();
        }
    }
}