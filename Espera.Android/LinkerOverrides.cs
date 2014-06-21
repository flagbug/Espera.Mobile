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
        }
    }
}