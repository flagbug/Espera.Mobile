using Splat;
using Xamarin.Forms;

namespace Espera.Mobile.Core.UI
{
    public static class XamFormsApp
    {
        public static INotification Notifications
        {
            get { return Locator.Current.GetService<INotification>(); }
        }

        public static Page GetMainPage()
        {
            return new NavigationPage(new MainPage());
        }
    }
}