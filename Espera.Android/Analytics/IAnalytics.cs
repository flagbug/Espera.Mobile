namespace Espera.Android.Analytics
{
    public interface IAnalytics
    {
        void RecordCustomMetric(string metric, string value);

        void RecordTiming(string category, long milliseconds, string name);
    }
}