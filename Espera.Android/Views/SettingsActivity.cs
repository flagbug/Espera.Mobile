using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Text;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core.Settings;
using Espera.Network;
using Google.Analytics.Tracking;
using Lager.Android;
using ReactiveUI;
using Xamarin.InAppBilling;

namespace Espera.Android.Views
{
    [Activity(Label = "Settings")]
    public class SettingsActivity : PreferenceActivity
    {
        private IInAppBillingHandler currentBillingHandler;

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            return AndroidVolumeRequests.Instance.HandleKeyCode(keyCode) || base.OnKeyDown(keyCode, e);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            // Xamarin.InAppBilling requires the codes to be passed to its handler
            if (this.currentBillingHandler != null)
            {
                this.currentBillingHandler.HandleActivityResult(requestCode, resultCode, data);
            }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.AddPreferencesFromResource(Resource.Layout.Settings);

            var portPref = (EditTextPreference)this.FindPreference(this.GetString(Resource.String.preference_port));
            portPref.EditText.InputType = InputTypes.ClassNumber;
            portPref.EditText.Events().TextChanged
                .Select(x => Int32.Parse(new string(x.Text.ToArray())))
                .Where(x => !NetworkHelpers.IsPortValid(x))
                .Subscribe(x =>
                {
                    portPref.EditText.Error = this.GetString(Resource.String.preference_port_validation_error);
                });
            portPref.BindToSetting(UserSettings.Instance, x => x.Port, x => x.Text, x => int.Parse(x.ToString()), x => x.ToString(), NetworkHelpers.IsPortValid);

            var ipAddressPref = (EditTextPreference)this.FindPreference(this.GetString(Resource.String.preference_ipaddress));
            ipAddressPref.EditText.InputType = InputTypes.ClassPhone;
            ipAddressPref.EditText.Events().TextChanged
                .Select(x => new string(x.Text.ToArray()))
                .Where(x => !IsValidIpAddress(x))
                .Subscribe(x =>
                {
                    ipAddressPref.EditText.Error = this.GetString(Resource.String.preference_ipaddress_validation_error);
                });

            ipAddressPref.BindToSetting(UserSettings.Instance, x => x.ServerAddress, x => x.Text, x => (string)x, x => x, IsValidIpAddress);

            var saveEnergyPref = (SwitchPreference)this.FindPreference(this.GetString(Resource.String.preference_save_energy));
            saveEnergyPref.BindToSetting(AndroidSettings.Instance, x => x.SaveEnergy, x => x.Checked, x => bool.Parse(x.ToString()));

            var adminEnabledPref = (SwitchPreference)this.FindPreference(this.GetString(Resource.String.preference_administrator_mode));
            adminEnabledPref.BindToSetting(UserSettings.Instance, x => x.EnableAdministratorMode, x => x.Checked, x => bool.Parse(x.ToString()));

            var passwordPreference = (EditTextPreference)this.FindPreference(this.GetString(Resource.String.preference_administrator_password));
            passwordPreference.BindToSetting(UserSettings.Instance, x => x.AdministratorPassword, x => x.Text, x => (string)x);
            UserSettings.Instance.WhenAnyValue(x => x.IsPremium).BindTo(passwordPreference, x => x.Enabled);

            var defaultLibraryActionPreference = (ListPreference)this.FindPreference(this.GetString(Resource.String.preference_default_library_action));
            defaultLibraryActionPreference.SetEntryValues(Enum.GetNames(typeof(DefaultLibraryAction)));
            defaultLibraryActionPreference.BindToSetting(UserSettings.Instance, x => x.DefaultLibraryAction,
                x => x.Value, x => Enum.Parse(typeof(DefaultLibraryAction), (string)x), x => x.ToString());
            UserSettings.Instance.WhenAnyValue(x => x.IsPremium).BindTo(defaultLibraryActionPreference, x => x.Enabled);

            Preference premiumButton = this.FindPreference("premium_button");
            premiumButton.Events().PreferenceClick.Subscribe(async _ => await this.PurchasePremium());
        }

        protected override void OnStart()
        {
            base.OnStart();

            EasyTracker.GetInstance(this).ActivityStart(this);
        }

        protected override void OnStop()
        {
            base.OnStop();

            EasyTracker.GetInstance(this).ActivityStop(this);
        }

        private static bool IsValidIpAddress(string address)
        {
            IPAddress dontCare;
            return String.IsNullOrEmpty(address) // An empty address indicates that we should auto-detect the server.
                || IPAddress.TryParse(address, out dontCare);
        }

        private async Task PurchasePremium()
        {
            var service = new InAppBillingServiceConnection(this, "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAh32oezB4EXDKOOSHGgH+H4P9mgKdXqx5ji1ndAhdw9gvSp3uPthav07MZTlQPjRq62+0eUgddosWjgMedMAs7Ov4QeOsmKsR40SOpICGDM0JBDXA7OE9HeJdr+yTeyC4yf7OsTZi6YKf8nFI68VkejLqv9Ell36aK/MczlTy5yJJhmgYUcLaZndYeUg4AVEhF7dK40TvPu/F7wuxVDqRYcoT1loiMNvYIt+/Wi3N7UAU07Uav+apwOnQHfkcWwb9PgZcpKuF7R2U3yWECoRgwAaXHoFmtBy9FomQ4uBEJlWIlg7TTAuK8Y3Ytlgnf02uFS4W1j0QjkErriEEWjm5TwIDAQAB");

            var connectedAwaiter = new TaskCompletionSource<Unit>();
            service.OnConnected += () => connectedAwaiter.SetResult(Unit.Default);

            service.Connect();

            await connectedAwaiter.Task;

            // We have to wait until the service is connected, otherwise the billing handler is null
            this.currentBillingHandler = service.BillingHandler;

            IList<Product> products = await service.BillingHandler.QueryInventoryAsync(new List<string> { "premium" }, ItemType.Product);

            var buyAwaiter = new TaskCompletionSource<int>();

            service.BillingHandler.OnProductPurchaseCompleted += (response, purchase) => buyAwaiter.SetResult(response);

            Product premium = products.Single(x => x.ProductId == "premium");
            service.BillingHandler.BuyProduct(premium);

            int billingResult = await buyAwaiter.Task;

            if (billingResult == BillingResult.OK)
            {
                UserSettings.Instance.IsPremium = true;
                Toast.MakeText(this, "Purchase successful!", ToastLength.Long).Show();
            }

            else
            {
                Toast.MakeText(this, "Purchase failed!", ToastLength.Long).Show();
            }

            var disconnectAwaiter = new TaskCompletionSource<Unit>();
            service.OnDisconnected += () => disconnectAwaiter.SetResult(Unit.Default);

            service.Disconnect();

            await disconnectAwaiter.Task;
        }
    }
}