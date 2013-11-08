using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Text;
using System;
using System.Linq;

namespace Espera.Android
{
    [Activity(Label = "Settings")]
    public class SettingsActivity : PreferenceActivity
    {
        public void OnSharedPreferenceChanged(ISharedPreferences sharedPreferences, string key)
        {
            throw new NotImplementedException();
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.AddPreferencesFromResource(Resource.Layout.Settings);

            var pref = (EditTextPreference)this.FindPreference("preference_port");
            pref.EditText.InputType = InputTypes.ClassNumber;
            pref.EditText.TextChanged += (sender, args) =>
            {
                int port = Int32.Parse(new string(args.Text.ToArray()));

                if (!IsValidPort(port))
                {
                    pref.EditText.Error = "Port must be between 49152 and 65535";
                }
            };
            pref.PreferenceChange += (sender, args) =>
            {
                int port = Int32.Parse(args.NewValue.ToString());
                args.Handled = IsValidPort(port);
            };
        }

        private static bool IsValidPort(int port)
        {
            return port > 49152 && port < 65535;
        }
    }
}