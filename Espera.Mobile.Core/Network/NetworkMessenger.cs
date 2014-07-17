using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Espera.Mobile.Core.Analytics;
using Espera.Mobile.Core.Songs;
using Espera.Network;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using Splat;

namespace Espera.Mobile.Core.Network
{
    public class NetworkMessenger : INetworkMessenger
    {
        private static Lazy<INetworkMessenger> instance;
        private readonly Subject<NetworkAccessPermission> accessPermission;
        private readonly IAnalytics analytics;
        private readonly Subject<ITcpClient> client;
        private readonly Subject<Unit> connectionEstablished;
        private readonly Subject<Unit> disconnected;
        private readonly SemaphoreSlim gate;
        private readonly IObservable<NetworkMessage> messagePipeline;
        private readonly IDisposable messagePipelineConnection;
        private ITcpClient currentClient;
        private ITcpClient currentFileTransferClient;

        static NetworkMessenger()
        {
            instance = new Lazy<INetworkMessenger>(() => new NetworkMessenger());
        }

        private NetworkMessenger()
        {
            this.gate = new SemaphoreSlim(1, 1);
            this.messagePipeline = new Subject<NetworkMessage>();
            this.disconnected = new Subject<Unit>();
            this.connectionEstablished = new Subject<Unit>();
            this.accessPermission = new Subject<NetworkAccessPermission>();

            this.analytics = Locator.Current.GetService<IAnalytics>();

            this.client = new Subject<ITcpClient>();

            var isConnected = this.Disconnected.Select(_ => false)
                .Merge(this.connectionEstablished.Select(_ => true))
                .DistinctUntilChanged()
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

            this.messagePipeline = pipeline.ObserveOn(RxApp.TaskpoolScheduler);
            this.messagePipelineConnection = pipeline.Connect();

            var pushMessages = this.messagePipeline.Where(x => x.MessageType == NetworkMessageType.Push)
                .Select(x => x.Payload.ToObject<PushInfo>());

            this.PlaylistChanged = pushMessages.Where(x => x.PushAction == PushAction.UpdateCurrentPlaylist)
                .Select(x => x.Content.ToObject<NetworkPlaylist>());

            this.PlaybackStateChanged = pushMessages.Where(x => x.PushAction == PushAction.UpdatePlaybackState)
                .Select(x => x.Content["state"].ToObject<NetworkPlaybackState>());

            this.PlaybackTimeChanged = pushMessages.Where(x => x.PushAction == PushAction.UpdateCurrentPlaybackTime)
                .Select(x => x.Content["currentPlaybackTime"].ToObject<TimeSpan>());

            this.RemainingVotesChanged = pushMessages.Where(x => x.PushAction == PushAction.UpdateRemainingVotes)
                .Select(x => x.Content["remainingVotes"].ToObject<int?>());

            var accessPermissionConn = pushMessages.Where(x => x.PushAction == PushAction.UpdateAccessPermission)
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

        public IObservable<TimeSpan> PlaybackTimeChanged { get; private set; }

        public IObservable<NetworkPlaylist> PlaylistChanged { get; private set; }

        public IObservable<int?> RemainingVotesChanged { get; private set; }

        /// <summary>
        /// Overrides the instance for unit testing.
        /// </summary>
        public static void Override(INetworkMessenger messenger)
        {
            instance = new Lazy<INetworkMessenger>(() => messenger);
        }

        public Task<ResponseInfo> AddSongToPlaylistAsync(Guid songGuid)
        {
            var parameters = new
            {
                songGuid
            };

            return this.SendRequest(RequestAction.AddPlaylistSongs, parameters);
        }

        /// <summary>
        /// Connects to the server with an optional password that requests administrator rights.
        /// </summary>
        /// <param name="ipAddress">The server's IP address.</param>
        /// <param name="port">The server's port.</param>
        /// <param name="deviceId">A, to this device unique identifier.</param>
        /// <param name="password">
        /// The optional administrator password. <c>null</c>, if guest rights are requested.
        /// </param>
        public async Task<Tuple<ResponseStatus, ConnectionInfo>> ConnectAsync(string ipAddress, int port, Guid deviceId, string password)
        {
            if (ipAddress == null)
                throw new ArgumentNullException("ipAddress");

            if (this.currentClient != null)
            {
                this.currentClient.Dispose();
            }

            if (this.currentFileTransferClient != null)
            {
                this.currentFileTransferClient.Dispose();
            }

            Func<ITcpClient> clientLocator = () => Locator.Current.GetService<ITcpClient>();

            this.currentClient = clientLocator();
            this.currentFileTransferClient = clientLocator();

            await this.currentClient.ConnectAsync(ipAddress, port);
            await this.currentFileTransferClient.ConnectAsync(ipAddress, port + 1);
            this.client.OnNext(this.currentClient);

            var parameters = new
            {
                deviceId,
                password
            };

            ResponseInfo response = await this.SendRequest(RequestAction.GetConnectionInfo, parameters);

            var connectionInfo = response.Content.ToObject<ConnectionInfo>();

            if (response.Status == ResponseStatus.Success)
            {
                this.connectionEstablished.OnNext(Unit.Default);
                this.accessPermission.OnNext(connectionInfo.AccessPermission);
            }

            return Tuple.Create(response.Status, connectionInfo);
        }

        public Task<ResponseInfo> ContinueSongAsync()
        {
            return this.SendRequest(RequestAction.ContinueSong);
        }

        public void Disconnect()
        {
            this.currentClient.Dispose();
            this.currentClient = null;

            this.currentFileTransferClient.Dispose();
            this.currentFileTransferClient = null;
        }

        public IObservable<string> DiscoverServerAsync(string localAddress, int port)
        {
            if (localAddress == null)
                throw new ArgumentNullException("localAddress");

            Func<IUdpClient> locatorFunc = () =>
            {
                var udpClient = Locator.Current.GetService<IUdpClient>();
                udpClient.Initialize(localAddress, port);
                return udpClient;
            };

            return Observable.Using(locatorFunc, x => Observable.FromAsync(x.ReceiveAsync))
                .Repeat()
                .FirstAsync(x => Encoding.Unicode.GetString(x.Item1, 0, x.Item1.Length) == NetworkConstants.DiscoveryMessage)
                .Select(x => x.Item2);
        }

        public void Dispose()
        {
            if (this.currentClient != null)
            {
                this.currentClient.Dispose();
            }

            if (this.currentFileTransferClient != null)
            {
                this.currentFileTransferClient.Dispose();
            }

            if (this.messagePipelineConnection != null)
            {
                this.messagePipelineConnection.Dispose();
            }
        }

        public async Task<NetworkPlaylist> GetCurrentPlaylistAsync()
        {
            ResponseInfo response = await this.SendRequest(RequestAction.GetCurrentPlaylist);

            return response.Content.ToObject<NetworkPlaylist>();
        }

        public async Task<IReadOnlyList<NetworkSong>> GetSongsAsync()
        {
            ResponseInfo response = await this.SendRequest(RequestAction.GetLibraryContent);

            return response.Content["songs"].ToObject<List<NetworkSong>>();
        }

        public async Task<float> GetVolume()
        {
            ResponseInfo response = await this.SendRequest(RequestAction.GetVolume);

            return response.Content["volume"].ToObject<float>();
        }

        public Task<ResponseInfo> MovePlaylistSongDownAsync(Guid entryGuid)
        {
            var parameters = new
            {
                entryGuid
            };

            return this.SendRequest(RequestAction.MovePlaylistSongDown, parameters);
        }

        public Task<ResponseInfo> MovePlaylistSongUpAsync(Guid entryGuid)
        {
            var parameters = new
            {
                entryGuid
            };

            return this.SendRequest(RequestAction.MovePlaylistSongUp, parameters);
        }

        public Task<ResponseInfo> PauseSongAsync()
        {
            return this.SendRequest(RequestAction.PauseSong);
        }

        public Task<ResponseInfo> PlayNextSongAsync()
        {
            return this.SendRequest(RequestAction.PlayNextSong);
        }

        public Task<ResponseInfo> PlayPlaylistSongAsync(Guid entryGuid)
        {
            var parameters = new
            {
                entryGuid
            };

            return this.SendRequest(RequestAction.PlayPlaylistSong, parameters);
        }

        public Task<ResponseInfo> PlayPreviousSongAsync()
        {
            return this.SendRequest(RequestAction.PlayPreviousSong);
        }

        public Task<ResponseInfo> PlaySongsAsync(IEnumerable<Guid> guids)
        {
            var parameters = new
            {
                guids = guids.Select(x => x.ToString())
            };

            return this.SendRequest(RequestAction.AddPlaylistSongsNow, parameters);
        }

        public async Task<FileTransferStatus> QueueRemoteSong(LocalSong songMetadata, byte[] songData)
        {
            var song = new NetworkSong
            {
                Album = songMetadata.Album,
                Artist = songMetadata.Artist,
                Duration = songMetadata.Duration,
                Genre = songMetadata.Genre,
                Source = NetworkSongSource.Mobile,
                Title = songMetadata.Title,
                Guid = Guid.NewGuid()
            };

            Guid transferId = Guid.NewGuid();
            var info = new SongTransferInfo
            {
                TransferId = transferId,
                Metadata = song
            };

            ResponseInfo response = await this.SendRequest(RequestAction.QueueRemoteSong, info);

            var message = new SongTransferMessage { Data = songData, TransferId = transferId };

            var progress = this.TransferFileAsync(message).Publish(0);
            progress.Connect();

            var status = new FileTransferStatus(progress);

            return status;
        }

        public Task<ResponseInfo> RemovePlaylistSongAsync(Guid entryGuid)
        {
            var parameters = new
            {
                entryGuid
            };

            return this.SendRequest(RequestAction.RemovePlaylistSong, parameters);
        }

        public Task<ResponseInfo> SetCurrentTime(TimeSpan time)
        {
            var parameters = new
            {
                time
            };

            return this.SendRequest(RequestAction.SetCurrentTime, parameters);
        }

        public Task<ResponseInfo> SetVolume(float volume)
        {
            if (volume < 0 || volume > 1)
                throw new ArgumentOutOfRangeException("volume");

            var parameters = new
            {
                volume
            };

            return this.SendRequest(RequestAction.SetVolume, parameters);
        }

        public Task<ResponseInfo> VoteAsync(Guid entryGuid)
        {
            var parameters = new
            {
                entryGuid
            };

            return this.SendRequest(RequestAction.VoteForSong, parameters);
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

        private async Task<ResponseInfo> SendRequest(RequestAction action, object parameters = null)
        {
            Guid id = Guid.NewGuid();

            var requestInfo = new RequestInfo
            {
                RequestAction = action,
                Parameters = parameters != null ? JObject.FromObject(parameters) : null,
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

            var stopwatch = Stopwatch.StartNew();

            try
            {
                await this.SendMessage(message);

                ResponseInfo response = await responseMessage;

                stopwatch.Stop();

                if (analytics != null)
                {
                    this.analytics.RecordNetworkTiming(action.ToString(), stopwatch.ElapsedMilliseconds);
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

        private IObservable<int> TransferFileAsync(SongTransferMessage message)
        {
            const int bufferSize = 32 * 1024;
            int written = 0;
            Stream stream = this.currentFileTransferClient.GetStream();

            var progress = new BehaviorSubject<int>(0);

            Task.Run(async () =>
            {
                byte[] data = await NetworkHelpers.PackFileTransferMessageAsync(message);

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