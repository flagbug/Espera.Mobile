using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Net;
using Android.Net.Wifi;
using Android.OS;
using Espera.Android.Views;
using Espera.Mobile.Core;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Settings;
using Espera.Network;
using ReactiveMarrow;
using ReactiveUI;
using Splat;
using Notification = Android.App.Notification;

namespace Espera.Android.Services
{
    [Service]
    internal class NetworkService : Service
    {
        public static readonly string ConnectionLostString = "connectionLost";
        private CompositeDisposable disposable;

        private PowerManager.WakeLock wakeLock;
        private WifiManager.WifiLock wifiLock;

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        public override void OnCreate()
        {
            base.OnCreate();

            this.disposable = new CompositeDisposable();

            this.wakeLock = PowerManager.FromContext(this).NewWakeLock(WakeLockFlags.Partial, "espera-wake-lock");
            this.wifiLock = WifiManager.FromContext(this).CreateWifiLock(WifiMode.Full, "espera-wifi-lock");

            NetworkMessenger.Instance.WhenAnyValue(x => x.IsConnected).Where(x => x)
                .Subscribe(x => this.NotifyNetworkMessengerConnected())
                .DisposeWith(this.disposable);

            NetworkMessenger.Instance.Disconnected.Subscribe(x => this.NotifyNetworkMessengerDisconnected())
                .DisposeWith(this.disposable);

            var userSettings = Locator.Current.GetService<UserSettings>();

            NetworkMessenger.Instance.WhenAnyValue(x => x.IsConnected).SampleAndCombineLatest(userSettings
                .WhenAnyValue(x => x.Port).DistinctUntilChanged(), (connected, _) => connected)
                .Where(x => x)
                .Subscribe(x => NetworkMessenger.Instance.Disconnect())
                .DisposeWith(this.disposable);

            AndroidVolumeRequests.Instance.VolumeDown.CombineLatest(NetworkMessenger.Instance.WhenAnyValue(x => x.IsConnected),
                    NetworkMessenger.Instance.WhenAnyValue(x => x.AccessPermission),
                    (_, connected, permission) => connected && permission == NetworkAccessPermission.Admin)
                .Where(x => x)
                .SelectMany(_ => NetworkMessenger.Instance.GetVolume().ToObservable().SwallowNetworkExceptions())
                .Where(currentVolume => currentVolume > 0)
                .Select(currentVolume => Math.Max(currentVolume - 0.1f, 0))
                .Select(volume => NetworkMessenger.Instance.SetVolume(volume).ToObservable().SwallowNetworkExceptions())
                .Concat()
                .Subscribe()
                .DisposeWith(this.disposable);

            AndroidVolumeRequests.Instance.VolumeUp.CombineLatest(NetworkMessenger.Instance.WhenAnyValue(x => x.IsConnected),
                    NetworkMessenger.Instance.WhenAnyValue(x => x.AccessPermission),
                    (_, connected, permission) => connected && permission == NetworkAccessPermission.Admin)
                .Where(x => x)
                .SelectMany(_ => NetworkMessenger.Instance.GetVolume().ToObservable().SwallowNetworkExceptions())
                .Where(currentVolume => currentVolume < 1)
                .Select(currentVolume => Math.Min(currentVolume + 0.1f, 1))
                .Select(volume => NetworkMessenger.Instance.SetVolume(volume).ToObservable().SwallowNetworkExceptions())
                .Concat()
                .Subscribe()
                .DisposeWith(this.disposable);

            var androidSettings = Locator.Current.GetService<AndroidSettings>();
            NetworkMessenger.Instance.WhenAnyValue(x => x.IsConnected).CombineLatest(androidSettings.WhenAnyValue(x => x.SaveEnergy), (connected, saveEnergy) =>
            {
                if (connected && !saveEnergy)
                {
                    this.wakeLock.Acquire();
                    this.wifiLock.Acquire();
                }

                else if (!connected)
                {
                    this.ReleaseWakeLocks();
                }

                return Unit.Default;
            })
            .Subscribe()
            .DisposeWith(this.disposable);
        }

        public override void OnDestroy()
        {
            this.disposable.Dispose();

            if (NetworkMessenger.Instance.IsConnected)
            {
                NetworkMessenger.Instance.Disconnect();
            }

            this.ReleaseWakeLocks();

            base.OnDestroy();
        }

        private void NotifyNetworkMessengerConnected()
        {
            var intent = new Intent(this, typeof(MainActivity)).SetAction(Intent.ActionMain).AddCategory(Intent.CategoryLauncher);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, 0);

            Notification notification = new Notification.Builder(this)
                .SetContentTitle(this.GetString(Resource.String.service_notification_title))
                .SetContentText(this.GetString(Resource.String.service_notification_text))
                .SetTicker(this.GetString(Resource.String.service_notification_text))
                .SetSmallIcon(Resource.Drawable.Play)
                .SetLargeIcon(BitmapFactory.DecodeResource(this.Resources, Resource.Drawable.Play))
                .SetContentIntent(pendingIntent)
                .SetOngoing(true)
                .Notification;

            this.StartForeground(6947, notification);
        }

        private void NotifyNetworkMessengerDisconnected()
        {
            this.StopForeground(true);

            var intent = new Intent(this, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
            intent.PutExtra(ConnectionLostString, true);

            this.StartActivity(intent);
        }

        private void ReleaseWakeLocks()
        {
            if (this.wakeLock.IsHeld)
            {
                this.wakeLock.Release();
            }

            if (this.wifiLock.IsHeld)
            {
                this.wifiLock.Release();
            }
        }
    }
}