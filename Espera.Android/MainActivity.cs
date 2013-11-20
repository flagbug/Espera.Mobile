using Android.App;
using Android.Content.PM;
using Android.Net.Wifi;
using Android.OS;
using Android.Preferences;
using Android.Text;
using Android.Widget;
using ReactiveUI;
using ReactiveUI.Android;
using ReactiveUI.Mobile;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Xamarin.ActionbarSherlockBinding;
using Xamarin.ActionbarSherlockBinding.Views;
using IMenuItem = Android.Views.IMenuItem;

namespace Espera.Android
{
    [Activity(Label = "Espera", MainLauncher = true, Icon = "@drawable/icon",
        ConfigurationChanges = ConfigChanges.Orientation)]
    public class MainActivity : ReactiveActivity<MainViewModel>, ActionBarSherlock.IOnCreateOptionsMenuListener
    {
        private readonly AutoSuspendActivityHelper autoSuspendHelper;
        private readonly BehaviorSubject<int> port;
        private readonly ActionBarSherlock sherlock;

        public MainActivity()
        {
            this.autoSuspendHelper = new AutoSuspendActivityHelper(this);
            this.sherlock = ActionBarSherlock.Wrap(this);

            this.port = new BehaviorSubject<int>(0);
        }

        private Button ConnectAsAdminButton
        {
            get { return this.FindViewById<Button>(Resource.Id.connectAsAdminButton); }
        }

        private Button ConnectButton
        {
            get { return this.FindViewById<Button>(Resource.Id.connectButton); }
        }

        private Button LoadArtistsButton
        {
            get { return this.FindViewById<Button>(Resource.Id.loadArtistsButton); }
        }

        private Button LoadCurrentPlaylistButton
        {
            get { return this.FindViewById<Button>(Resource.Id.loadCurrentPlaylistButton); }
        }

        public override bool OnCreateOptionsMenu(global::Android.Views.IMenu menu)
        {
            return this.sherlock.DispatchCreateOptionsMenu(menu);
        }

        public bool OnCreateOptionsMenu(IMenu menu)
        {
            menu.Add("Settings").SetIcon(Resource.Drawable.Settings)
                .SetShowAsAction(MenuItem.ShowAsActionAlways | MenuItem.ShowAsActionWithText);

            return true;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            this.StartActivity(typeof(SettingsActivity));

            return true;
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            this.autoSuspendHelper.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Main);

            PreferenceManager.SetDefaultValues(this, Resource.Layout.Settings, false);

            if (this.Intent.HasExtra("connectionLost"))
            {
                Toast.MakeText(this, "Connection lost", ToastLength.Long).Show();
            }

            var wifiManager = (WifiManager)this.GetSystemService(WifiService);
            if (!wifiManager.IsWifiEnabled)
            {
                var builder = new AlertDialog.Builder(this);
                builder.SetTitle("Error");
                builder.SetMessage("You have to enable Wifi.");
                builder.SetPositiveButton("Enable", (sender, args) => wifiManager.SetWifiEnabled(true));
                builder.SetNegativeButton("Exit", (sender, args) => this.Finish());

                builder.Show();
            }

            this.ViewModel = new MainViewModel(this.port);
            this.BindCommand(this.ViewModel, x => x.ConnectCommand, x => x.ConnectButton);
            this.ViewModel.ConnectCommand.IsExecuting
                .Select(x => x ? "Connecting..." : "Connect As Guest")
                .BindTo(this.ConnectButton, x => x.Text);

            this.ConnectAsAdminButton.Click += (sender, args) => this.ShowAdminPasswordPrompt();
            this.ViewModel.ConnectAsAdminCommand.IsExecuting
                .Select(x => x ? "Connecting..." : "Connect As Admin")
                .BindTo(this.ConnectAsAdminButton, x => x.Text);
            this.ViewModel.ConnectAsAdminCommand.IsExecuting
                .Select(x => !x)
                .BindTo(this.ConnectAsAdminButton, x => x.Enabled);

            this.ViewModel.ConnectionFailed.Subscribe(x => Toast.MakeText(this, "Connection failed", ToastLength.Long).Show());
            this.ViewModel.WrongPassword.Subscribe(x => Toast.MakeText(this, "Wrong password", ToastLength.Long).Show());

            this.OneWayBind(this.ViewModel, x => x.IsConnected, x => x.LoadArtistsButton.Enabled);
            this.LoadArtistsButton.Click += (sender, args) => this.StartActivity(typeof(ArtistsActivity));

            this.OneWayBind(this.ViewModel, x => x.IsConnected, x => x.LoadCurrentPlaylistButton.Enabled);
            this.LoadCurrentPlaylistButton.Click += (sender, args) => this.StartActivity(typeof(PlaylistActivity));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            NetworkMessenger.Instance.Dispose();
        }

        protected override void OnPause()
        {
            base.OnPause();
            this.autoSuspendHelper.OnPause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            this.autoSuspendHelper.OnResume();

            string portString = PreferenceManager.GetDefaultSharedPreferences(this).GetString("preference_port", null);
            this.port.OnNext(Int32.Parse(portString));
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            this.autoSuspendHelper.OnSaveInstanceState(outState);
        }

        private void ShowAdminPasswordPrompt()
        {
            var builder = new AlertDialog.Builder(this);

            builder.SetTitle("Password");

            var input = new EditText(this)
            {
                InputType = InputTypes.MaskClass
            };
            builder.SetView(input);

            builder.SetPositiveButton("Ok", (o, eventArgs) =>
            {
                this.ViewModel.Password = input.Text;

                this.ViewModel.ConnectAsAdminCommand.Execute(null);
            });

            builder.SetNegativeButton("Cancel", (o, eventArgs) => { });

            builder.Show();
        }
    }
}