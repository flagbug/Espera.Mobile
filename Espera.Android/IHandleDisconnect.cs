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

    internal static class HandleDisconnectExtension
    {
        public static void HandleDisconnect<T>(this T activity) where T : Activity, IHandleDisconnect
        {
            var intent = new Intent(activity, typeof(MainActivity));
            intent.SetFlags(ActivityFlags.ClearTop);
            intent.PutExtra("connectionLost", true);

            activity.StartActivity(intent);
        }
    }
}