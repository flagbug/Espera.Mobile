using ReactiveUI;
using Xamarin.Forms;

namespace Espera.Mobile.Core.UI
{
    public static class XamFormsApp
    {
        static XamFormsApp()
        {
            RxApp.MainThreadScheduler = new DeviceScheduler();
        }

        public static Page GetMainPage()
        {
            return new NavigationPage(new MainPage());
        }
    }
}