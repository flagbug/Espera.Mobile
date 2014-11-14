using Espera.Mobile.Core.Network;
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
        /// Catches exceptions of type <see cref="NetworkException" /> and
        /// <see cref="NetworkRequestException" /> and returns an empty observable instead.
        /// </summary>
        public static IObservable<T> SwallowNetworkExceptions<T>(this IObservable<T> source)
        {
            return source
                .Catch<T, NetworkException>(ex => Observable.Empty<T>())
                .Catch<T, NetworkRequestException>(ex => Observable.Empty<T>());
        }

        /// <summary>
        /// Returns elements from the source sequence until the specified
        /// <see cref="CompositeDisposable" /> is disposed.
        /// </summary>
        public static IObservable<T> TakeUntil<T>(this IObservable<T> source, CompositeDisposable disposable)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));

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