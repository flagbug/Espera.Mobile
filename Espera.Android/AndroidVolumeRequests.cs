using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Espera.Mobile.Core;

namespace Espera.Android
{
    internal class AndroidVolumeRequests : IVolumeRequests
    {
        private readonly Subject<Unit> volumeDown;
        private readonly Subject<Unit> volumeUp;
        private static readonly Lazy<AndroidVolumeRequests> instance;

        static AndroidVolumeRequests()
        {
            instance = new Lazy<AndroidVolumeRequests>(() => new AndroidVolumeRequests());
        }

        public static AndroidVolumeRequests Instance
        {
            get { return instance.Value; }
        }

        private AndroidVolumeRequests()
        {
            this.volumeDown = new Subject<Unit>();
            this.volumeUp = new Subject<Unit>();
        }

        public IObservable<Unit> VolumeDown
        {
            get { return this.volumeDown.AsObservable(); }
        }

        public IObservable<Unit> VolumeUp
        {
            get { return this.volumeUp.AsObservable(); }
        }

        private void RaiseVolumeDown()
        {
            this.volumeDown.OnNext(Unit.Default);
        }

        private void RaiseVolumeUp()
        {
            this.volumeUp.OnNext(Unit.Default);
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
    }
}