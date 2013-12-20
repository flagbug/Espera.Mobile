using Android.App;
using Android.Content;
using Android.OS;
using Espera.Android.Views;
using System;
using System.Reactive.Linq;

namespace Espera.Android.Network
{
    [Service]
    internal class NetworkService : Service
    {
        private const int NotificationId = 0;
        private INetworkMessenger keepAlive;

        public override IBinder OnBind(Intent intent)
        {
            throw new NotImplementedException();
        }

        public override void OnCreate()
        {
            base.OnCreate();

            this.keepAlive = NetworkMessenger.Instance;
            this.keepAlive.IsConnected.Where(x => x)
                .Subscribe(x => this.NotifyNetworkMessengerConnected());

            this.keepAlive.Disconnected.Subscribe(x => this.NotifyNetworkMessengerDisconnected());
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            keepAlive.Disconnect();
            keepAlive.Dispose();
        }

        private void NotifyNetworkMessengerConnected()
        {
            var messenger = (NotificationManager)this.GetSystemService(NotificationService);
            var notification = new Notification(Resource.Drawable.Play, "Espera is connected");
            var pendingIntent = PendingIntent.GetActivity(this, 0, new Intent(this, typeof(MainActivity)), 0);
            notification.SetLatestEventInfo(this, "Espera Network", "Espera is connected", pendingIntent);

            messenger.Notify(NotificationId, notification);
        }

        private void NotifyNetworkMessengerDisconnected()
        {
            var messenger = (NotificationManager)this.GetSystemService(NotificationService);
            messenger.Cancel(NotificationId);
        }
    }
}