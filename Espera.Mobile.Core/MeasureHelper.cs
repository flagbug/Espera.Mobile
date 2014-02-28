using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;

namespace Espera.Mobile.Core
{
    public static class MeasureHelper
    {
        public static IDisposable Measure([CallerMemberName] string caller = null)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            return Disposable.Create(() =>
            {
                stopWatch.Stop();
                Console.WriteLine("Measured in {0}: {1}", caller, stopWatch.Elapsed);
            });
        }
    }
}