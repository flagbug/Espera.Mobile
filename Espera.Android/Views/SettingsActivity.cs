using Android.App;
using Android.OS;
using Android.Preferences;
using Android.Text;
using Android.Widget;
using Espera.Android.Settings;
using Lager.Android;
using ReactiveUI;
using System;
using System.Linq;

namespace Espera.Android.Views
{
    [Activity(Label = "Settings")]
    public class SettingsActivity : PreferenceActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.AddPreferencesFromResource(Resource.Layout.Settings);

            var portPref = (EditTextPreference)this.FindPreference(this.GetString(Resource.String.preference_port));
            portPref.EditText.InputType = InputTypes.ClassNumber;
            portPref.EditText.Events().TextChanged.Subscribe(x =>
            {
                int port = Int32.Parse(new string(x.Text.ToArray()));

                if (!IsValidPort(port))
                {
                    portPref.EditText.Error = this.GetString(Resource.String.preference_port_validation_error);
                }
            });
            portPref.BindToSetting(UserSettings.Instance, x => x.Port, x => x.Text, x => int.Parse(x.ToString()), x => x.ToString(), IsValidPort);

            var adminEnabledPref = (CheckBoxPreference)this.FindPreference(this.GetString(Resource.String.preference_enable_administrator_mode));
            adminEnabledPref.BindToSetting(UserSettings.Instance, x => x.EnableAdministratorMode, x => x.Checked, x => bool.Parse(x.ToString()));

            var passwordPreference = (EditTextPreference)this.FindPreference(this.GetString(Resource.String.preference_administrator_password));
            passwordPreference.BindToSetting(UserSettings.Instance, x => x.AdministratorPassword, x => x.Text, x => (string)x);
            UserSettings.Instance.WhenAnyValue(x => x.EnableAdministratorMode).BindTo(passwordPreference, x => x.Enabled);

            var defaultLibraryActionPreference = (ListPreference)this.FindPreference(this.GetString(Resource.String.preference_default_library_action));
            defaultLibraryActionPreference.SetEntryValues(Enum.GetNames(typeof(DefaultLibraryAction)));
            defaultLibraryActionPreference.BindToSetting(UserSettings.Instance, x => x.DefaultLibraryAction,
                x => x.Value, x => Enum.Parse(typeof(DefaultLibraryAction), (string)x), x => x.ToString());
            UserSettings.Instance.WhenAnyValue(x => x.EnableAdministratorMode).BindTo(defaultLibraryActionPreference, x => x.Enabled);
        }

        private static bool IsValidPort(int port)
        {
            return port > 49152 && port < 65535;
        }
    }
}