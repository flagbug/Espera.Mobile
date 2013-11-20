using Android.App;
using Android.Content;
using Espera.Android.Views;

namespace Espera.Android
{
    /// <summary>
    /// Decorator for the HandleDisconnect extension method
    /// </summary>
    internal interface IHandleDisconnect
    { }

    public static class HandleDisconnectExtension
    {
        public static void HandleDisconnect(this Activity activity)
        {
            var intent = new Intent(activity, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.ClearTop);
            intent.PutExtra("connectionLost", true);

            activity.StartActivity(intent);
        }
    }
}