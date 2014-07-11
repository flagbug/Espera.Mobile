using System;
using System.Reactive;

namespace Espera.Mobile.Core
{
    public interface IVolumeRequests
    {
        IObservable<Unit> VolumeDown { get; }

        IObservable<Unit> VolumeUp { get; }
    }
}