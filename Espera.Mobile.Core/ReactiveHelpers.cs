using System;
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
    }
}