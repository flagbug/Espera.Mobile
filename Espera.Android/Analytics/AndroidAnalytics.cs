using Android.Content;
using Google.Analytics.Tracking;
using System;

namespace Espera.Android.Analytics
{
    public class AndroidAnalytics : IAnalytics
    {
        private readonly EasyTracker tracker;

        public AndroidAnalytics(Context context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            this.tracker = EasyTracker.GetInstance(context);
        }

        public void RecordTiming(string category, long milliseconds, string name)
        {
            var dic = MapBuilder.CreateTiming(category, new Java.Lang.Long(milliseconds), name, null).Build();

            this.tracker.Send(dic);
        }
    }
}