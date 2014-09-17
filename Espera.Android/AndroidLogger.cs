using System;
using Android.Util;
using Splat;

namespace Espera.Android
{
    internal class AndroidLogger : ILogger
    {
        public LogLevel Level { get; set; }

        public void Write(string message, LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                    Log.Debug(String.Empty, message);
                    break;

                case LogLevel.Error:
                    Log.Error(String.Empty, message);
                    break;

                case LogLevel.Fatal:
                    Log.Error(String.Empty, message);
                    break;

                case LogLevel.Info:
                    Log.Info(String.Empty, message);
                    break;

                case LogLevel.Warn:
                    Log.Warn(String.Empty, message);
                    break;
            }
        }
    }
}