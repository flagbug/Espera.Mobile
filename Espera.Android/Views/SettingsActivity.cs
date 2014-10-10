using Android.App;
using Android.OS;
using Android.Preferences;
using Android.Views;

namespace Espera.Android.Views
{
    [Activity]
    public class SettingsActivity : PreferenceActivity
    {
        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            return AndroidVolumeRequests.Instance.HandleKeyCode(keyCode) || base.OnKeyDown(keyCode, e);
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.FragmentManager.BeginTransaction()
                .Replace(global::Android.Resource.Id.Content, new SettingsFragment())
                .Commit();
        }
    }
}