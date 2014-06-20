using Android.Views;
using Android.Widget;

namespace Espera.Android
{
    internal class Linker
    {
        public Linker()
        {
            var linearLayout = new LinearLayout(null);
            linearLayout.Visibility = ViewStates.Visible;
        }
    }
}