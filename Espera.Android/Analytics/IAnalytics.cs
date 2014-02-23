namespace Espera.Android.Analytics
{
    public interface IAnalytics
    {
        void RecordTiming(string category, long milliseconds, string name);
    }

    public static class AnalyticsExtensions
    {
        public static void RecordNetworkTiming(this IAnalytics analytics, string networkAction, long milliseconds)
        {
            analytics.RecordTiming("network", milliseconds, networkAction);
        }
    }
}