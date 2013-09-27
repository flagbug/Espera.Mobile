using Android.App;
using Android.Content;

namespace Espera.Android
{
    /// <summary>
    /// Decorator for the HandleDisconnect exetsnion method
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