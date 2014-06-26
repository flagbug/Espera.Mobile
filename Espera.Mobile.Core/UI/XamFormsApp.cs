using Xamarin.Forms;

namespace Espera.Mobile.Core.UI
{
    public static class XamFormsApp
    {
        public static Page GetMainPage()
        {
            return new NavigationPage(new MainPage());
        }
    }
}