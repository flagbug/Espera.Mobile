using Akavache;
using Lager;
using Splat;

namespace Espera.Android
{
    public class AndroidSettings : SettingsStorage
    {
        public AndroidSettings()
            : base("__AndroidSettings__", ModeDetector.InUnitTestRunner() ? new InMemoryBlobCache() : BlobCache.UserAccount)
        { }

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