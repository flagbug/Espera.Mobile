using System;
using System.Text;
using Android.Content;
using Android.Provider;
using Espera.Mobile.Core;

namespace Espera.Android
{
    internal class AndroidDeviceIdFactory : IDeviceIdFactory
    {
        private readonly Context context;

        public AndroidDeviceIdFactory(Context context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            this.context = context;
        }

        public Guid GetDeviceId()
        {
            string uuid = Settings.Secure.GetString(this.context.ContentResolver, Settings.Secure.AndroidId);
            byte[] bytes = Encoding.UTF8.GetBytes(uuid);

            return new Guid(bytes);
        }
    }
}