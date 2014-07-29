using System;
using Akavache;
using Espera.Network;
using Lager;

namespace Espera.Mobile.Core.Settings
{
    public class UserSettings : SettingsStorage
    {
        private static readonly Lazy<UserSettings> instance;

        static UserSettings()
        {
            instance = new Lazy<UserSettings>(() => new UserSettings());
        }

        private UserSettings()
            : base("__Settings__", BlobCache.LocalMachine)
        { }

        public static UserSettings Instance
        {
            get { return instance.Value; }
        }

        public string AdministratorPassword
        {
            get { return this.GetOrCreate((string)null); }
            set { this.SetOrCreate(value); }
        }

        public DefaultLibraryAction DefaultLibraryAction
        {
            get { return this.GetOrCreate(DefaultLibraryAction.PlayAll); }
            set { this.SetOrCreate(value); }
        }

        public bool EnableAdministratorMode
        {
            get { return this.GetOrCreate(false); }
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

        public string UniqueIdentifier
        {
            get { return this.GetOrCreate(Guid.NewGuid().ToString()); }
            set { this.SetOrCreate(value); }
        }
    }
}