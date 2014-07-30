using System;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using Android.App;
using Android.OS;
using Android.Preferences;
using Android.Text;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core.Settings;
using Espera.Network;
using Google.Analytics.Tracking;
using Lager.Android;
using ReactiveUI;

namespace Espera.Android.Views
{
    [Activity(Label = "Settings")]
    public class SettingsActivity : PreferenceActivity
    {
        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            return AndroidVolumeRequests.Instance.HandleKeyCode(keyCode) || base.OnKeyDown(keyCode, e);
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.AddPreferencesFromResource(Resource.Layout.Settings);

            var portPref = (EditTextPreference)this.FindPreference(this.GetString(Resource.String.preference_port));
            portPref.EditText.InputType = InputTypes.ClassNumber;
            portPref.EditText.Events().TextChanged
                .Select(x => Int32.Parse(new string(x.Text.ToArray())))
                .Where(x => !NetworkHelpers.IsPortValid(x))
                .Subscribe(x =>
                {
                    portPref.EditText.Error = this.GetString(Resource.String.preference_port_validation_error);
                });
            portPref.BindToSetting(UserSettings.Instance, x => x.Port, x => x.Text, x => int.Parse(x.ToString()), x => x.ToString(), NetworkHelpers.IsPortValid);

            var ipAddressPref = (EditTextPreference)this.FindPreference(this.GetString(Resource.String.preference_ipaddress));
            ipAddressPref.EditText.InputType = InputTypes.ClassPhone;
            ipAddressPref.EditText.Events().TextChanged
                .Select(x => new string(x.Text.ToArray()))
                .Where(x => !IsValidIpAddress(x))
                .Subscribe(x =>
                {
                    ipAddressPref.EditText.Error = this.GetString(Resource.String.preference_ipaddress_validation_error);
                });
            ipAddressPref.BindToSetting(UserSettings.Instance, x => x.ServerAddress, x => x.Text, x => (string)x, x => x, IsValidIpAddress);

            var adminEnabledPref = (SwitchPreference)this.FindPreference(this.GetString(Resource.String.preference_administrator_mode));
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

        protected override void OnStart()
        {
            base.OnStart();

            EasyTracker.GetInstance(this).ActivityStart(this);
        }

        protected override void OnStop()
        {
            base.OnStop();

            EasyTracker.GetInstance(this).ActivityStop(this);
        }

        private static bool IsValidIpAddress(string address)
        {
            IPAddress dontCare;
            return String.IsNullOrEmpty(address) // An empty address indicates that we should auto-detect the server.
                || IPAddress.TryParse(address, out dontCare);
        }
    }
}