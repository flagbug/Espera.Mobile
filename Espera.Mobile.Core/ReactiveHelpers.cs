using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
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

        /// <summary>
        /// Returns elements from the source sequence until the specified
        /// <see cref="CompositeDisposable" /> is disposed.
        /// </summary>
        public static IObservable<T> TakeUntil<T>(this IObservable<T> source, CompositeDisposable disposable)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (disposable == null)
                throw new ArgumentNullException("disposable");

            var subject = new AsyncSubject<T>();

            disposable.Add(Disposable.Create(() =>
            {
                subject.OnNext(default(T));
                subject.OnCompleted();
            }));

            return source.TakeUntil(subject);
        }

        /// <summary>
        /// Skips elements from the <paramref name="source" /> sequence as soon as an element of the
        /// <paramref name="throttler" /> sequence arrives, for the specified duration.
        /// </summary>
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