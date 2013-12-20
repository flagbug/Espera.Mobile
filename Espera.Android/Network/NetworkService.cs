using Android.App;
using Android.Content;
using Android.OS;
using Espera.Android.Settings;
using Espera.Android.Views;
using ReactiveUI;
using System;
using System.Reactive.Linq;

namespace Espera.Android.Network
{
    [Service]
    internal class NetworkService : Service
    {
        private INetworkMessenger keepAlive;

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

            UserSettings.Instance.WhenAnyValue(x => x.Port).DistinctUntilChanged()
                .CombineLatestValue(NetworkMessenger.Instance.IsConnected, (p, connected) => connected)
                .Where(x => x)
                .Subscribe(x => NetworkMessenger.Instance.Disconnect());
        }

        public override void OnDestroy()
        {
            keepAlive.Disconnect();
            keepAlive.Dispose();

            base.OnDestroy();
        }

        private void NotifyNetworkMessengerConnected()
        {
            var notification = new Notification(Resource.Drawable.Play, "Espera is connected");
            var pendingIntent = PendingIntent.GetActivity(this, 0, new Intent(this, typeof(MainActivity)), 0);
            notification.SetLatestEventInfo(this, "Espera Network", "Espera is connected", pendingIntent);

            this.StartForeground((int)NotificationFlags.ForegroundService, notification);
        }

        private void NotifyNetworkMessengerDisconnected()
        {
            this.StopForeground(true);

            var intent = new Intent(this, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
            intent.PutExtra("connectionLost", true);

            this.StartActivity(intent);
        }
    }
}