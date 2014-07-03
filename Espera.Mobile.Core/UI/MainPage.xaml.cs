using System;
using System.Reactive.Linq;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.ViewModels;
using ReactiveUI;
using Splat;
using Xamarin.Forms;

namespace Espera.Mobile.Core.UI
{
    public partial class MainPage : ContentPage, IViewFor<MainViewModel>
    {
        public static readonly BindableProperty ViewModelProperty =
            BindableProperty.Create<MainPage, MainViewModel>(x => x.ViewModel, null);

        public MainPage()
        {
            this.InitializeComponent();

            var wifiService = Locator.Current.GetService<IWifiService>();

            this.ViewModel = new MainViewModel(wifiService.GetIpAddress);
            this.ViewModel.Activator.Activate();

            var connectOrDisconnectCommand = this.ViewModel.WhenAnyValue(x => x.IsConnected)
                .Select(x => x ? (IReactiveCommand)this.ViewModel.DisconnectCommand : this.ViewModel.ConnectCommand);
            connectOrDisconnectCommand.BindTo(this.ConnectButton, x => x.Command);

            this.ViewModel.ConnectCommand.IsExecuting
                .CombineLatest(this.ViewModel.WhenAnyValue(x => x.IsConnected), (connecting, connected) =>
                    connected ? "Disconnect" : connecting ? "Connecting..." : "Connect")
                .BindTo(this.ConnectButton, x => x.Text);

            this.ViewModel.ConnectionFailed.Subscribe(XamFormsApp.Notifications.Notify);

            this.OneWayBind(this.ViewModel, x => x.IsConnected, x => x.RemoteArtistsButton.IsEnabled);
            this.RemoteArtistsButton.Clicked += async (sender, e) => await this.Navigation.PushAsync(new RemoteArtistsPage());

            this.OneWayBind(this.ViewModel, x => x.IsConnected, x => x.PlaylistButton.IsEnabled);
            this.PlaylistButton.Clicked += async (sender, e) => await this.Navigation.PushAsync(new PlaylistPage());

            this.OneWayBind(this.ViewModel, x => x.IsConnected, x => x.LocalArtistsButton.IsEnabled);
            this.LocalArtistsButton.Clicked += async (sender, e) => await this.Navigation.PushAsync(new LocalArtistsPage());
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (MainViewModel)value; }
        }

        public MainViewModel ViewModel
        {
            get { return (MainViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
    }
}