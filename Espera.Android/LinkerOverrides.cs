using Android.Preferences;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.ComponentModel;

namespace Espera.Android
{
    [Preserve(AllMembers = true)]
    internal class LinkerOverrides
    {
        private void KeepMe()
        {
            var boolConverter = new BooleanConverter();
            var stringConverter = new StringConverter();
            var enumConverter = new EnumConverter(null);
            var intConverter = new Int32Converter();

            var linearLayout = new LinearLayout(null);
            linearLayout.Visibility = ViewStates.Visible;

            var seekBar = new SeekBar(null);
            seekBar.Max = seekBar.Max;
            seekBar.Progress = seekBar.Progress;

            var textPref = new EditTextPreference(null);
            textPref.Text = textPref.Text;
            textPref.Enabled = textPref.Enabled;

            var switchPref = new SwitchPreference(null);
            switchPref.Checked = switchPref.Checked;
            switchPref.Enabled = switchPref.Enabled;

            var listPref = new ListPreference(null);
            listPref.Enabled = listPref.Enabled;
            listPref.Value = listPref.Value;
        }
    }
}