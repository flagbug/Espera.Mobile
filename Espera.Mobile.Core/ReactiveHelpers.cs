using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Espera.Mobile.Core
{
    public static class ReactiveHelpers
    {
        public static IObservable<T> PermaRef<T>(this IConnectableObservable<T> observable)
        {
            observable.Connect();
            return observable;
        }

        public static IObservable<T> ThrottleWhenIncoming<T, TDontCare>(this IObservable<T> source, IObservable<TDontCare> throttler, TimeSpan throttleDuration, IScheduler scheduler)
        {
            bool acceptElements = true;

            throttler.Do(_ => acceptElements = false)
                .Throttle(throttleDuration, scheduler)
                .Subscribe(_ => acceptElements = true);

            return source.Where(_ => acceptElements);
        }
    }
}