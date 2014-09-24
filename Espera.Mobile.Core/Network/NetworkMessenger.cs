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
using Espera.Mobile.Core.Settings;
using Espera.Network;
using Newtonsoft.Json.Linq;
using ReactiveUI;
using Splat;

namespace Espera.Mobile.Core.Network
{
    public class NetworkMessenger : ReactiveObject, INetworkMessenger
    {
        private static Lazy<INetworkMessenger> instance;
        private readonly ObservableAsPropertyHelper<NetworkAccessPermission> accessPermission;
        private readonly IAnalytics analytics;
        private readonly Subject<ITcpClient> client;
        private readonly Subject<Unit> connectionEstablished;
        private readonly Subject<ConnectionInfo> connectionInfoReceived;
        private readonly Subject<Unit> disconnected;
        private readonly SemaphoreSlim gate;
        private readonly ObservableAsPropertyHelper<GuestSystemInfo> guestSystemInfo;
        private readonly ObservableAsPropertyHelper<bool> isConnected;
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
            this.connectionInfoReceived = new Subject<ConnectionInfo>();

            this.analytics = Locator.Current.GetService<IAnalytics>();

            this.client = new Subject<ITcpClient>();

            this.isConnected = this.Disconnected.Select(_ => false)
                .Merge(this.connectionEstablished.Select(_ => true))
                .StartWith(false)
                .Do(x => this.Log().Info("Connection status: {0}", x ? "Connected" : "Disconnected"))
                .ToProperty(this, x => x.IsConnected);
            var connectConn = this.IsConnected;

            var pipeline = this.client.Select(x => Observable.Defer(() => x.GetStream().ReadNextMessageAsync()
                    .ToObservable())
                    .Repeat()
                    .LoggedCatch(this, null, "Error while reading the next network message")
                    .TakeWhile(m => m != null)
                    .TakeUntil(this.Disconnected))
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

            var settings = Locator.Current.GetService<UserSettings>();

            if (settings == null)
            {
                throw new InvalidOperationException("No user settings registered!");
            }

            this.accessPermission = pushMessages.Where(x => x.PushAction == PushAction.UpdateAccessPermission)
                .Select(x => x.Content["accessPermission"].ToObject<NetworkAccessPermission>())
                .Merge(this.connectionInfoReceived.Select(x => x.AccessPermission))
                .Select(x => TrialHelpers.GetAccessPermissionForPremiumState(x, settings.IsPremium ||
                    TrialHelpers.IsInTrialPeriod(AppConstants.TrialTime)))
                .ToProperty(this, x => x.AccessPermission);
            var connectAccessPermission = this.AccessPermission;

            this.guestSystemInfo = pushMessages.Where(x => x.PushAction == PushAction.UpdateGuestSystemInfo)
                .Select(x => x.Content.ToObject<GuestSystemInfo>())
                .Merge(this.connectionInfoReceived.Select(x => x.GuestSystemInfo))
                .ToProperty(this, x => x.GuestSystemInfo);
            var connectGuestSystemInfo = this.GuestSystemInfo;
        }

        public static INetworkMessenger Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Gets the current access permission of the current user.
        /// </summary>
        public NetworkAccessPermission AccessPermission
        {
            get { return this.accessPermission.Value; }
        }

        public IObservable<Unit> Disconnected
        {
            get { return this.disconnected.AsObservable(); }
        }

        public GuestSystemInfo GuestSystemInfo
        {
            get { return this.guestSystemInfo.Value; }
        }

        public bool IsConnected
        {
            get { return this.isConnected.Value; }
        }

        public IObservable<NetworkPlaybackState> PlaybackStateChanged { get; private set; }

        public IObservable<TimeSpan> PlaybackTimeChanged { get; private set; }

        public IObservable<NetworkPlaylist> PlaylistChanged { get; private set; }

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
                guids = new[] { songGuid } // The server expects an enumeration of songs
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
        /// The optional administrator password. <c>null</c> , if guest rights are requested.
        /// </param>
        /// <exception cref="NetworkException">
        /// Something went wrong while connecting to the server.
        /// </exception>
        /// <returns>
        /// A <see cref="Tuple"/> of a <see cref="ResponseStatus" /> and <see cref="ConnectionInfo" />.
        ///
        /// If there was an error on the server side (e.g a wrong password), the
        /// <see cref="ConnectionInfo" /> is null.
        /// </returns>
        public async Task<ConnectionResultContainer> ConnectAsync(string ipAddress, int port, Guid deviceId, string password)
        {
            if (ipAddress == null)
                throw new ArgumentNullException("ipAddress");

            if (this.IsConnected)
            {
                this.Disconnect();
            }

            Func<ITcpClient> clientLocator = () => Locator.Current.GetService<ITcpClient>();

            this.currentClient = clientLocator();
            this.currentFileTransferClient = clientLocator();

            this.Log().Info("Connecting to the Espera host at {0}", ipAddress);

            this.Log().Info("Connecting the message client at port {0}", port);
            await this.currentClient.ConnectAsync(ipAddress, port);

            this.Log().Info("Connecting the file transfer client at port {0}", port + 1);
            await this.currentFileTransferClient.ConnectAsync(ipAddress, port + 1);
            this.client.OnNext(this.currentClient);

            var parameters = new
            {
                deviceId,
                password
            };

            this.Log().Info("Everything connected, requesting the connection info");

            ResponseInfo response = await this.SendRequest(RequestAction.GetConnectionInfo, parameters);

            if (response.Status == ResponseStatus.WrongPassword)
            {
                this.Log().Error("Server said: wrong password");

                return new ConnectionResultContainer(ConnectionResult.WrongPassword);
            }

            var connectionInfo = response.Content.ToObject<ConnectionInfo>();

            if (connectionInfo.ServerVersion < AppConstants.MinimumServerVersion)
            {
                this.Log().Error("Server has version {0}, but version {1} is required", connectionInfo.ServerVersion, AppConstants.MinimumServerVersion);

                return new ConnectionResultContainer(ConnectionResult.ServerVersionToLow, null, connectionInfo.ServerVersion);
            }

            if (response.Status == ResponseStatus.Success)
            {
                this.connectionInfoReceived.OnNext(connectionInfo);

                // Notify the connection status at the very end or bad things happen
                this.connectionEstablished.OnNext(Unit.Default);

                this.Log().Info("Connection to server established");

                return new ConnectionResultContainer(ConnectionResult.Successful, connectionInfo.AccessPermission, connectionInfo.ServerVersion);
            }

            throw new InvalidOperationException("We shouldn't reach this code");
        }

        public Task<ResponseInfo> ContinueSongAsync()
        {
            return this.SendRequest(RequestAction.ContinueSong);
        }

        public void Disconnect()
        {
            this.Log().Info("Disconnecting from the network");

            if (this.currentClient != null)
            {
                this.currentClient.Dispose();
                this.currentClient = null;
            }

            if (this.currentFileTransferClient != null)
            {
                this.currentFileTransferClient.Dispose();
                this.currentClient = null;
            }

            if (this.IsConnected)
            {
                this.Log().Info("Notifying of disconnection");
                this.disconnected.OnNext(Unit.Default);
            }
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

            this.Log().Info("Starting server discovery at port {0}...", port);

            return Observable.Using(locatorFunc, x => Observable.FromAsync(x.ReceiveAsync))
                .Repeat()
                .TakeWhile(x => x != null)
                .FirstAsync(x => Encoding.Unicode.GetString(x.Item1, 0, x.Item1.Length) == NetworkConstants.DiscoveryMessage)
                .Select(x => x.Item2)
                .Do(x => this.Log().Info("Detected server at IP address {0}", x));
        }

        public void Dispose()
        {
            this.Disconnect();

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

            using (MeasureHelper.Measure("Deserialization in GetSongsAsync"))
            {
                // In a big library, deserializing can take a longer time, so we do this in its own thread
                return await Task.Run(() => response.Content["songs"].ToObject<List<NetworkSong>>());
            }
        }

        public async Task<IReadOnlyList<NetworkSong>> GetSoundCloudSongsAsync(string searchTerm)
        {
            var parameters = new
            {
                searchTerm
            };

            ResponseInfo response = await this.SendRequest(RequestAction.GetSoundCloudSongs, parameters);

            if (response.Status == ResponseStatus.Success)
            {
                return response.Content["songs"].ToObject<List<NetworkSong>>();
            }

            return new List<NetworkSong>();
        }

        public async Task<float> GetVolume()
        {
            ResponseInfo response = await this.SendRequest(RequestAction.GetVolume);

            return response.Content["volume"].ToObject<float>();
        }

        public async Task<IReadOnlyList<NetworkSong>> GetYoutubeSongsAsync(string searchTerm)
        {
            var parameters = new
            {
                searchTerm
            };

            ResponseInfo response = await this.SendRequest(RequestAction.GetYoutubeSongs, parameters);

            if (response.Status == ResponseStatus.Success)
            {
                return response.Content["songs"].ToObject<List<NetworkSong>>();
            }

            return new List<NetworkSong>();
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

            if (response.Status != ResponseStatus.Success)
            {
                return new FileTransferStatus(response);
            }

            var message = new SongTransferMessage { Data = songData, TransferId = transferId };

            var progress = this.TransferFileAsync(message).Publish(0);
            progress.Connect();

            var status = new FileTransferStatus(response, progress);

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

        public Task<ResponseInfo> ToggleVideoPlayer()
        {
            return this.SendRequest(RequestAction.ToggleYoutubePlayer);
        }

        public Task<ResponseInfo> VoteAsync(Guid entryGuid)
        {
            var parameters = new
            {
                entryGuid
            };

            return this.SendRequest(RequestAction.VoteForSong, parameters);
        }

        private IObservable<ResponseInfo> GetResponsePipeline()
        {
            return this.messagePipeline
                .Where(x => x.MessageType == NetworkMessageType.Response)
                .Select(x => x.Payload.ToObject<ResponseInfo>());
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

            Task<ResponseInfo> responseMessage = this.GetResponsePipeline()
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

                this.Log().ErrorException("Fatal error while sending or receiving a network response", ex);

                this.Disconnect();

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
                this.Log().Info("Starting a file transfer with ID: {0} and a size of {1} bytes", message.TransferId, message.Data.Length);

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