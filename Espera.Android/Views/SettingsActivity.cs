using System;
using System.Linq;
using Android.App;
using Android.OS;
using Android.Preferences;
using Android.Text;

namespace Espera.Android.Views
{
    [Activity(Label = "Settings")]
    public class SettingsActivity : PreferenceActivity
    {
        private Preference DefaultLibraryActionPreference
        {
            get { return this.FindPreference(this.GetString(Resource.String.preference_default_library_action)); }
        }

        private Preference PasswordPreference
        {
            get { return this.FindPreference(this.GetString(Resource.String.preference_administrator_password)); }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.AddPreferencesFromResource(Resource.Layout.Settings);

            var portPref = (EditTextPreference)this.FindPreference(this.GetString(Resource.String.preference_port));
            portPref.EditText.InputType = InputTypes.ClassNumber;
            portPref.EditText.TextChanged += (sender, args) =>
            {
                int port = Int32.Parse(new string(args.Text.ToArray()));

                if (!IsValidPort(port))
                {
                    portPref.EditText.Error = "Port must be between 49152 and 65535";
                }
            };
            portPref.PreferenceChange += (sender, args) =>
            {
                int port = Int32.Parse(args.NewValue.ToString());
                args.Handled = IsValidPort(port);
            };

            var adminEnabledPref = this.FindPreference(this.GetString(Resource.String.preference_enable_administrator_mode));
            adminEnabledPref.PreferenceChange += (sender, args) =>
            {
                bool enabled = bool.Parse(args.NewValue.ToString());
                args.Handled = true;

                this.UpdateAdminPreferences(enabled);
            };

            this.UpdateAdminPreferences(PreferenceManager.SharedPreferences
                .GetBoolean(this.GetString(Resource.String.preference_enable_administrator_mode), false));
        }

        private static bool IsValidPort(int port)
        {
            return port > 49152 && port < 65535;
        }

        private void UpdateAdminPreferences(bool enabled)
        {
            this.PasswordPreference.Enabled = enabled;
            this.DefaultLibraryActionPreference.Enabled = enabled;
        }
    }
}