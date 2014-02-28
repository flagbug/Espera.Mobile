using System;
using System.Diagnostics;
using System.Reactive.Disposables;

namespace Espera.Mobile.Core
{
    public static class MeasureHelper
    {
        public static IDisposable Measure()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            return Disposable.Create(() =>
            {
                stopWatch.Stop();
                Console.WriteLine("Measured: {0}", stopWatch.Elapsed);
            });
        }
    }
}