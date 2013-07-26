using Android.App;
using Android.OS;
using ReactiveUI;

namespace Espera.Android
{
    [Activity(Label = "Espera", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, IViewFor<MainViewModel>
    {
        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (MainViewModel)value; }
        }

        public MainViewModel ViewModel { get; set; }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            this.ViewModel = new MainViewModel();
        }
    }
}