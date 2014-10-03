using ReactiveUI;
using Splat;
using Xamarin.Forms;
using Espera.Mobile.Core.Views;

namespace Espera.Mobile.Core.ViewModels
{
    public class AppBootstrapper : ReactiveObject, IScreen
    {
        public AppBootstrapper()
        {
            this.Router = new RoutingState();

            Locator.CurrentMutable.RegisterConstant(this, typeof(IScreen));
        }

        public RoutingState Router { get; private set; }

        public Page CreateMainPage()
        {
			return new NavigationPage (new MainPage ());
        }
    }
}