using System;

using Android.Content;
using Android.Widget;
using Espera.Mobile.Core.UI;

namespace Espera.Android
{
    public class ToastNotification : INotification
    {
        private readonly Context context;

        public ToastNotification(Context context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            this.context = context;
        }

        public void Notify(string message)
        {
            var toast = Toast.MakeText(this.context, message, ToastLength.Short);
            toast.Show();
        }
    }
}