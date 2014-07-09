using Android.Content;
using Espera.Mobile.Core.Analytics;
using System;

namespace Espera.Android.Analytics
{
    public class AndroidAnalytics : IAnalytics
    {
        public AndroidAnalytics(Context context)
        {
            if (context == null)
                throw new ArgumentNullException("context");
        }

        public void RecordCustomMetric(int metric, string value)
        { }

        public void RecordTiming(string category, long milliseconds, string name)
        { }
    }
}