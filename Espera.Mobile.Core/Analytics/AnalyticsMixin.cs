using System.Globalization;

namespace Espera.Android.Analytics
{
    public static class AnalyticsMixin
    {
        public static void RecordNetworkTiming(this IAnalytics analytics, string networkAction, long milliseconds)
        {
            analytics.RecordTiming("network", milliseconds, networkAction);
        }

        public static void RecordWifiSpeed(this IAnalytics analytics, int speed)
        {
            analytics.RecordCustomMetric(1, speed.ToString(CultureInfo.InvariantCulture));
        }
    }
}