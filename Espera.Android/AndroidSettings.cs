using System;
using Akavache;
using Lager;

namespace Espera.Android
{
    public class AndroidSettings : SettingsStorage
    {
        private static readonly Lazy<AndroidSettings> instance;

        static AndroidSettings()
        {
            instance = new Lazy<AndroidSettings>(() => new AndroidSettings());
        }

        public AndroidSettings()
            : base("__AndroidSettings__", BlobCache.UserAccount)
        { }

        public static AndroidSettings Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// If this value is set to true, we don't aquire a wakelock.
        /// </summary>
        public bool SaveEnergy
        {
            get { return this.GetOrCreate(false); }
            set { this.SetOrCreate(value); }
        }
    }
}