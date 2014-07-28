using Android.Content;
using Android.Widget;
using Espera.Mobile.Core.UI;
using System;

namespace Espera.Android
{
    internal class AndroidNotification : INotification
    {
        private readonly Context context;

        public AndroidNotification(Context context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            this.context = context;
        }

        public void Notify(string message)
        {
            Toast.MakeText(this.context, message, ToastLength.Short).Show();
        }
    }
}