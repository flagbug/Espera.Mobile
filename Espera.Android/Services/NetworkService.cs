using System;
using System.Reactive.Linq;
using Android.App;
using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Espera.Android.Views;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Settings;
using Espera.Network;
using ReactiveMarrow;
using ReactiveUI;
using Notification = Android.App.Notification;

namespace Espera.Android.Services
{
    [Service]
    internal class NetworkService : Service
    {
        private INetworkMessenger keepAlive;

        private PowerManager.WakeLock wakeLock;
        private WifiManager.WifiLock wifiLock;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override void OnCreate()
        {
            base.OnCreate();

            this.keepAlive = NetworkMessenger.Instance;
            this.keepAlive.IsConnected.Where(x => x)
                .Subscribe(x => this.NotifyNetworkMessengerConnected());

            this.keepAlive.Disconnected.Subscribe(x => this.NotifyNetworkMessengerDisconnected());

            NetworkMessenger.Instance.IsConnected.SampleAndCombineLatest(UserSettings.Instance
                .WhenAnyValue(x => x.Port).DistinctUntilChanged(), (connected, _) => connected)
                .Where(x => x)
                .Subscribe(x => NetworkMessenger.Instance.Disconnect());

            AndroidVolumeRequests.Instance.VolumeDown.CombineLatest(NetworkMessenger.Instance.IsConnected, NetworkMessenger.Instance.AccessPermission,
                    (_, connected, permission) => connected && permission == NetworkAccessPermission.Admin)
                .Where(x => x)
                .SelectMany(async _ => await NetworkMessenger.Instance.GetVolume())
                .Where(currentVolume => currentVolume > 0)
                .Select(currentVolume => Math.Max(currentVolume - 0.1f, 0))
                .Select(async volume => await NetworkMessenger.Instance.SetVolume(volume))
                .Concat()
                .Subscribe();

            AndroidVolumeRequests.Instance.VolumeUp.CombineLatest(NetworkMessenger.Instance.IsConnected, NetworkMessenger.Instance.AccessPermission,
                    (_, connected, permission) => connected && permission == NetworkAccessPermission.Admin)
                .Where(x => x)
                .SelectMany(async _ => await NetworkMessenger.Instance.GetVolume())
                .Where(currentVolume => currentVolume < 1)
                .Select(currentVolume => Math.Min(currentVolume + 0.1f, 1))
                .Select(async volume => await NetworkMessenger.Instance.SetVolume(volume))
                .Concat()
                .Subscribe();
        }

        public override void OnDestroy()
        {
            keepAlive.Disconnect();
            keepAlive.Dispose();

            base.OnDestroy();
        }

        private void NotifyNetworkMessengerConnected()
        {
            this.wakeLock = PowerManager.FromContext(this).NewWakeLock(WakeLockFlags.Partial, "espera-wake-lock");
            this.wakeLock.Acquire();

            this.wifiLock = WifiManager.FromContext(this).CreateWifiLock(WifiMode.Full, "espera-wifi-lock");
            this.wifiLock.Acquire();

            var notification = new Notification(Resource.Drawable.Play, "Espera is connected");
            var intent = new Intent(this, typeof(MainActivity)).SetAction(Intent.ActionMain).AddCategory(Intent.CategoryLauncher);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, 0);
            notification.SetLatestEventInfo(this, "Espera Network", "Espera is connected", pendingIntent);
            notification.Flags |= NotificationFlags.OngoingEvent;

            this.StartForeground(6947, notification);
        }

        private void NotifyNetworkMessengerDisconnected()
        {
            this.wakeLock.Release();
            this.wifiLock.Release();

            this.StopForeground(true);

            var intent = new Intent(this, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
            intent.PutExtra("connectionLost", true);

            this.StartActivity(intent);
        }
    }
}