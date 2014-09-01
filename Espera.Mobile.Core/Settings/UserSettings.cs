using System;
using Akavache;
using Espera.Network;
using Lager;
using Splat;

namespace Espera.Mobile.Core.Settings
{
    public class UserSettings : SettingsStorage
    {
        public UserSettings()
            : base("__Settings__", ModeDetector.InUnitTestRunner() ? new InMemoryBlobCache() : BlobCache.UserAccount)
        { }

        public string AdministratorPassword
        {
            get { return this.GetOrCreate((string)null); }
            set { this.SetOrCreate(value); }
        }

        public bool IsPremium
        {
#if DEBUG || DEV
            get { return true; }
#else
            get { return this.GetOrCreate(false); }
#endif
            set { this.SetOrCreate(value); }
        }

        public int Port
        {
            get { return this.GetOrCreate(NetworkConstants.DefaultPort); }
            set { this.SetOrCreate(value); }
        }

        /// <summary>
        /// If set, override the auto-detection of the server's address.
        /// </summary>
        public string ServerAddress
        {
            get { return this.GetOrCreate((string)null); }
            set { this.SetOrCreate(value); }
        }

        /// <summary>
        /// The unique ID we assign this device and use on the server.
        /// </summary>
        public Guid UniqueIdentifier
        {
            get { return this.GetOrCreate(GetUniqueId()); }
            set { this.SetOrCreate(value); }
        }

        private Guid GetUniqueId()
        {
            var service = Locator.Current.GetService<IDeviceIdFactory>();

            if (service == null && ModeDetector.InUnitTestRunner())
            {
                return new Guid();
            }

            if (service == null)
            {
                throw new InvalidOperationException("There isn't a device ID factory registered!");
            }

            return service.GetDeviceId();
        }
    }
}