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
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Songs;
using Espera.Network;
using Newtonsoft.Json.Linq;
using ReactiveUI;

namespace Espera.Mobile.Networking
{
    /// <summary>
    /// This class works around the fact that we have no sockets in the Portable Class Library
    /// </summary>
    public class NetworkMessengerImpl : IDisposable, INetworkMessenger
    {
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

        public NetworkMessengerImpl()
        {
            this.gate = new SemaphoreSlim(1, 1);
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

            this.PlaylistChanged = pushMessages.Where(x => x.PushAction == PushAction.UpdateCurrentPlaylist)
                .Select(x => x.Content.ToObject<NetworkPlaylist>());

            this.PlaybackStateChanged = pushMessages.Where(x => x.PushAction == PushAction.UpdatePlaybackState)
                .Select(x => x.Content["state"].ToObject<NetworkPlaybackState>());

            this.RemainingVotesChanged = pushMessages.Where(x => x.PushAction == PushAction.UpdateRemainingVotes)
                .Select(x => x.Content["remainingVotes"].ToObject<int?>());

            var accessPermissionConn = pushMessages.Where(x => x.PushAction == PushAction.UpdateAccessPermission)
                .Select(x => x.Content["accessPermission"].ToObject<NetworkAccessPermission>())
                .Merge(this.accessPermission)
                .Publish(NetworkAccessPermission.Guest);
            accessPermissionConn.Connect();

            this.AccessPermission = accessPermissionConn;
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

        public async Task<ResponseInfo> AddSongToPlaylistAsync(Guid songGuid)
        {
            var parameters = JObject.FromObject(new
            {
                songGuid
            });

            ResponseInfo response = await this.SendRequest(RequestAction.AddPlaylistSongs, parameters);

            return response;
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

            var address = IPAddress.Parse(ipAddress);

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

            ResponseInfo response = await this.SendRequest(RequestAction.GetConnectionInfo, parameters);

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
            ResponseInfo response = await this.SendRequest(RequestAction.ContinueSong);

            return response;
        }

        public void Disconnect()
        {
            this.currentClient.Close();
            this.currentClient = null;
        }

        public Task<string> DiscoverServerAsync(string localAddress, int port)
        {
            if (localAddress == null)
                throw new ArgumentNullException("localAddress");

            return Observable.Using(() => new UdpClient(new IPEndPoint(IPAddress.Parse(localAddress), port)), x => Observable.FromAsync(x.ReceiveAsync))
                .Repeat()
                .FirstAsync(x => Encoding.Unicode.GetString(x.Buffer) == NetworkConstants.DiscoveryMessage)
                .Select(x => x.RemoteEndPoint.Address.ToString())
                .ToTask();
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

        public async Task<NetworkPlaylist> GetCurrentPlaylistAsync()
        {
            ResponseInfo response = await this.SendRequest(RequestAction.GetCurrentPlaylist);

            return response.Content.ToObject<NetworkPlaylist>();
        }

        public async Task<IReadOnlyList<NetworkSong>> GetSongsAsync()
        {
            ResponseInfo response = await this.SendRequest(RequestAction.GetLibraryContent);

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

            ResponseInfo response = await this.SendRequest(RequestAction.MovePlaylistSongDown, parameters);

            return response;
        }

        public async Task<ResponseInfo> MovePlaylistSongUpAsync(Guid entryGuid)
        {
            var parameters = JObject.FromObject(new
            {
                entryGuid
            });

            ResponseInfo response = await this.SendRequest(RequestAction.MovePlaylistSongUp, parameters);

            return response;
        }

        public async Task<ResponseInfo> PauseSongAsync()
        {
            ResponseInfo response = await this.SendRequest(RequestAction.PauseSong);

            return response;
        }

        public async Task<ResponseInfo> PlayNextSongAsync()
        {
            ResponseInfo response = await this.SendRequest(RequestAction.PlayNextSong);

            return response;
        }

        public async Task<ResponseInfo> PlayPlaylistSongAsync(Guid entryGuid)
        {
            var parameters = JObject.FromObject(new
            {
                entryGuid
            });

            ResponseInfo response = await this.SendRequest(RequestAction.PlayPlaylistSong, parameters);

            return response;
        }

        public async Task<ResponseInfo> PlayPreviousSongAsync()
        {
            ResponseInfo response = await this.SendRequest(RequestAction.PlayPreviousSong);

            return response;
        }

        public async Task<ResponseInfo> PlaySongsAsync(IEnumerable<Guid> guids)
        {
            var parameters = JObject.FromObject(new
            {
                guids = guids.Select(x => x.ToString())
            });

            ResponseInfo response = await this.SendRequest(RequestAction.AddPlaylistSongsNow, parameters);

            return response;
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

            ResponseInfo response = await this.SendRequest(RequestAction.QueueRemoteSong, JObject.FromObject(info));

            var message = new SongTransferMessage { Data = songData, TransferId = transferId };

            var progress = this.TransferFileAsync(message).Publish(0);
            progress.Connect();

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

            ResponseInfo response = await this.SendRequest(RequestAction.RemovePlaylistSong, parameters);

            return response;
        }

        public async Task<ResponseInfo> VoteAsync(Guid entryGuid)
        {
            var parameters = JObject.FromObject(new
            {
                entryGuid
            });

            ResponseInfo response = await this.SendRequest(RequestAction.VoteForSong, parameters);

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

        private async Task<ResponseInfo> SendRequest(RequestAction action, JObject parameters = null)
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