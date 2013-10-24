using Android.App;
using Android.OS;
using Android.Preferences;
using Android.Text;

namespace Espera.Android
{
    [Activity(Label = "Settings")]
    public class SettingsActivity : PreferenceActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.AddPreferencesFromResource(Resource.Layout.Settings);

            var pref = (EditTextPreference)this.FindPreference("preference_port");
            pref.EditText.InputType = InputTypes.ClassNumber;
        }
    }
}