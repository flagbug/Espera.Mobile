using Espera.Mobile.Core;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Espera.Android
{
    internal class AndroidVolumeRequests : IVolumeRequests
    {
        private static readonly Lazy<AndroidVolumeRequests> instance;
        private readonly Subject<Unit> volumeDown;
        private readonly Subject<Unit> volumeUp;

        static AndroidVolumeRequests()
        {
            instance = new Lazy<AndroidVolumeRequests>(() => new AndroidVolumeRequests());
        }

        private AndroidVolumeRequests()
        {
            this.volumeDown = new Subject<Unit>();
            this.volumeUp = new Subject<Unit>();
        }

        public static AndroidVolumeRequests Instance
        {
            get { return instance.Value; }
        }

        public IObservable<Unit> VolumeDown
        {
            get { return this.volumeDown.AsObservable(); }
        }

        public IObservable<Unit> VolumeUp
        {
            get { return this.volumeUp.AsObservable(); }
        }

        public bool HandleKeyCode(global::Android.Views.Keycode keyCode)
        {
            switch (keyCode)
            {
                case global::Android.Views.Keycode.VolumeDown:
                    this.RaiseVolumeDown();
                    return true;

                case global::Android.Views.Keycode.VolumeUp:
                    this.RaiseVolumeUp();
                    return true;
            }

            return false;
        }

        private void RaiseVolumeDown() => this.volumeDown.OnNext(Unit.Default);

        private void RaiseVolumeUp() => this.volumeUp.OnNext(Unit.Default);
    }
}