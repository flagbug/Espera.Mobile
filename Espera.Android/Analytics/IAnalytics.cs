using Google.Analytics.Tracking;
using System.Globalization;

namespace Espera.Android.Analytics
{
    public interface IAnalytics
    {
        void RecordCustomMetric(string metric, string value);

        void RecordTiming(string category, long milliseconds, string name);
    }

    public static class AnalyticsExtensions
    {
        public static void RecordNetworkTiming(this IAnalytics analytics, string networkAction, long milliseconds)
        {
            analytics.RecordTiming("network", milliseconds, networkAction);
        }

        public static void RecordWifiSpeed(this IAnalytics analytics, int speed)
        {
            analytics.RecordCustomMetric(Fields.CustomMetric(1), speed.ToString(CultureInfo.InvariantCulture));
        }
    }
}