using System;
using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.OS;
using Android.Views;
using Android.Widget;
using Espera.Android.Services;
using Espera.Mobile.Core;
using Espera.Mobile.Core.Analytics;
using Espera.Mobile.Core.Network;
using Espera.Mobile.Core.Settings;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
using Google.Analytics.Tracking;
using Humanizer;
using ReactiveMarrow;
using ReactiveUI;
using Splat;
using IMenuItem = Android.Views.IMenuItem;

namespace Espera.Android.Views
{
    [Activity(Label = "Espera", MainLauncher = true, Icon = "@drawable/icon",
        ConfigurationChanges = ConfigChanges.Orientation, LaunchMode = LaunchMode.SingleTop)]
    public class MainActivity : ReactiveActivity<MainViewModel>
    {
        public MainActivity()
        {
            var settings = Locator.Current.GetService<UserSettings>();
            var wifiService = Locator.Current.GetService<IWifiService>();

            this.ViewModel = new MainViewModel(settings, wifiService.GetIpAddress);

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
                            return Resources.GetString(Resource.String.enable_wifi);
                    }

                    throw new InvalidOperationException("This shouldn't happen");
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => Toast.MakeText(this, x, ToastLength.Long).Show())
                .DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.IsConnected, x => x.LoadPlaylistButton.Enabled)
                    .DisposeWith(disposable);
                this.LoadPlaylistButton.Events().Click.Subscribe(x => this.StartActivity(typeof(PlaylistActivity)))
                    .DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.IsConnected, x => x.LoadRemoteArtistsButton.Enabled)
                    .DisposeWith(disposable);
                this.LoadRemoteArtistsButton.Events().Click.Subscribe(x => this.StartActivity(typeof(RemoteArtistsActivity)))
                    .DisposeWith(disposable);

                this.OneWayBind(this.ViewModel, x => x.IsConnected, x => x.LoadLocalArtistsButton.Enabled);
                this.LoadLocalArtistsButton.Events().Click.Subscribe(x => this.StartActivity(typeof(LocalArtistsActivity)))
                    .DisposeWith(disposable);

                bool displayTrialPeriod = TrialHelpers.IsInTrialPeriod(AppConstants.TrialTime) && !settings.IsPremium;
                this.TrialExpirationTextView.Visibility = this.TrialExpirationExplanationTextview.Visibility =
                    displayTrialPeriod ? ViewStates.Visible : ViewStates.Gone;

                if (displayTrialPeriod)
                {
                    TimeSpan remainingTrialTime = TrialHelpers.GetRemainingTrialTime(AppConstants.TrialTime);
                    this.TrialExpirationTextView.Text = string.Format("{0} {1}",
                        Resources.GetString(Resource.String.trial_expiration),
                        remainingTrialTime.Humanize(culture: new CultureInfo("en-US")));

                    this.TrialExpirationExplanationTextview.Text = Resources.GetString(Resource.String.trial_expiration_explanation);
                }

                return disposable;
            });
        }

        public Button ConnectButton { get; private set; }

        public Button LoadLocalArtistsButton { get; private set; }

        public Button LoadPlaylistButton { get; private set; }

        public Button LoadRemoteArtistsButton { get; private set; }

        public TextView TrialExpirationExplanationTextview { get; private set; }

        public TextView TrialExpirationTextView { get; private set; }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            menu.Add(Resources.GetString(Resource.String.settings))
                .SetIcon(Resource.Drawable.Settings)
                .SetShowAsAction(ShowAsAction.Always);

            return true;
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            return AndroidVolumeRequests.Instance.HandleKeyCode(keyCode) || base.OnKeyDown(keyCode, e);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            this.StartActivity(typeof(SettingsActivity));

            return true;
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.Title = String.Empty;

            this.SetContentView(Resource.Layout.Main);
            this.WireUpControls();
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            this.Intent = intent;
        }

        protected override void OnResume()
        {
            base.OnResume();

            if (this.Intent.HasExtra(NetworkService.ConnectionLostString))
            {
                Toast.MakeText(this, Resource.String.connection_lost, ToastLength.Long).Show();
                this.Intent.RemoveExtra(NetworkService.ConnectionLostString);
            }

            var wifiService = Locator.Current.GetService<IWifiService>();

            if (wifiService.GetIpAddress() == null)
            {
                this.ShowWifiPrompt();
            }

            else
            {
                var analytics = Locator.Current.GetService<IAnalytics>();
                analytics.RecordWifiSpeed(wifiService.GetWifiSpeed());
            }
        }

        protected override void OnStart()
        {
            base.OnStart();

            EasyTracker.GetInstance(this).ActivityStart(this);

            this.StartService(new Intent(this, typeof(NetworkService)));
        }

        protected override void OnStop()
        {
            base.OnStop();

            EasyTracker.GetInstance(this).ActivityStop(this);

            if (!NetworkMessenger.Instance.IsConnected)
            {
                this.StopService(new Intent(this, typeof(NetworkService)));
            }
        }

        private void ShowWifiPrompt()
        {
            var wifiManager = WifiManager.FromContext(this);
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle(Resource.String.error);
            builder.SetMessage(Resource.String.enable_wifi);
            builder.SetPositiveButton(Resource.String.enable, (sender, args) => wifiManager.SetWifiEnabled(true));
            builder.SetNegativeButton(Resource.String.exit, (sender, args) => this.Finish());

            builder.Show();
        }
    }
}