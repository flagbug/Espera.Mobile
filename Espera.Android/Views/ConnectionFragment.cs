using System;
using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.OS;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Settings;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
using Humanizer;
using ReactiveMarrow;
using ReactiveUI;
using Splat;
using Xamarin;

namespace Espera.Android.Views
{
    public class ConnectionFragment : ReactiveFragment<ConnectionViewModel>
    {
        public ConnectionFragment()
        {
            var settings = Locator.Current.GetService<UserSettings>();
            var wifiService = Locator.Current.GetService<IWifiService>();

            this.ViewModel = new ConnectionViewModel(settings, wifiService.GetIpAddress);

            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                var connectOrDisconnectCommand = this.ViewModel.WhenAnyValue(x => x.IsConnected)
                    .Select(x => x ? (IReactiveCommand)this.ViewModel.DisconnectCommand : this.ViewModel.ConnectCommand);

                connectOrDisconnectCommand.SampleAndCombineLatest(this.ConnectButton.Events().Click, (command, args) => command)
                    .Where(x => x.CanExecute(null))
                    .Subscribe(x => x.Execute(null))
                    .DisposeWith(disposable);

                connectOrDisconnectCommand.SelectMany(x => x.CanExecuteObservable)
                    .BindTo(this.ConnectButton, x => x.Enabled)
                    .DisposeWith(disposable);

                this.ViewModel.ConnectCommand.IsExecuting
                    .CombineLatest(this.ViewModel.WhenAnyValue(x => x.IsConnected), (connecting, connected) =>
                        connected ? Resource.String.disconnect : connecting ? Resource.String.connecting : Resource.String.connect)
                    .Select(Resources.GetString)
                    .BindTo(this.ConnectButton, x => x.Text)
                    .DisposeWith(disposable);

                this.ViewModel.ConnectCommand.Select(result =>
                {
                    switch (result.ConnectionResult)
                    {
                        case ConnectionResult.Failed:
                            return Resources.GetString(Resource.String.connection_failed);

                        case ConnectionResult.ServerVersionToLow:
                            return string.Format(Resources.GetString(Resource.String.required_server_version), result.ServerVersion.ToString(3));

                        case ConnectionResult.Successful:
                            {
                                var playlistFragment = new PlaylistFragment();

                                this.FragmentManager.BeginTransaction()
                                    .Replace(Resource.Id.ContentFrame, playlistFragment)
                                    .Commit();

                                switch (result.AccessPermission)
                                {
                                    case NetworkAccessPermission.Admin:
                                        return Resources.GetString(Resource.String.connected_as_admin);

                                    case NetworkAccessPermission.Guest:
                                        return Resources.GetString(Resource.String.connected_as_guest);
                                }
                                break;
                            }

                        case ConnectionResult.Timeout:
                            return Resources.GetString(Resource.String.connection_timeout);

                        case ConnectionResult.WrongPassword:
                            return Resources.GetString(Resource.String.wrong_password);

                        case ConnectionResult.WifiDisabled:
                            return Resources.GetString(Resource.String.wifi_enable_error);
                    }

                    throw new InvalidOperationException("This shouldn't happen");
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => Toast.MakeText(this.Activity, x, ToastLength.Long).Show())
                .DisposeWith(disposable);

                TimeSpan remainingTrialTime = TrialHelpers.GetRemainingTrialTime(AppConstants.TrialTime);

                // We don't want to immediately scare the user with the display of the remaining trial period, so give it some time
                bool displayTrialPeriod = !settings.IsPremium && remainingTrialTime < TimeSpan.FromDays(4);
                this.TrialExpirationTextView.Visibility = this.TrialExpirationExplanationTextview.Visibility =
                    displayTrialPeriod ? ViewStates.Visible : ViewStates.Gone;

                if (displayTrialPeriod)
                {
                    string expirationMessage = remainingTrialTime > TimeSpan.Zero ?
                        Resources.GetString(Resource.String.trial_expiration) :
                        Resources.GetString(Resource.String.trial_expiration_expired);

                    this.TrialExpirationTextView.Text = string.Format(expirationMessage,
                        remainingTrialTime.Duration().Humanize(culture: new CultureInfo("en-US")));

                    this.TrialExpirationExplanationTextview.Text = Resources.GetString(Resource.String.trial_expiration_explanation);
                }

                return disposable;
            });
        }

        public Button ConnectButton { get; private set; }

        public TextView TrialExpirationExplanationTextview { get; private set; }

        public TextView TrialExpirationTextView { get; private set; }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.ConnectionLayout, null);

            this.WireUpControls(view);

            return view;
        }

        public override void OnResume()
        {
            base.OnResume();

            this.Activity.SetTitle(Resource.String.main_fragment_title);
        }

        public override void OnStart()
        {
            base.OnStart();

            Insights.Track(this.GetType().Name);
        }
    }
}