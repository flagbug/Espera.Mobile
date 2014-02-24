namespace Espera.Mobile.Core.Analytics
{
    public interface IAnalytics
    {
        void RecordCustomMetric(int metric, string value);

        void RecordTiming(string category, long milliseconds, string name);
    }
}