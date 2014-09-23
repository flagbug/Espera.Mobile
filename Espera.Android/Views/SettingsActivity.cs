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
using Espera.Mobile.Core;
using Espera.Mobile.Core.Settings;
using Espera.Network;
using Google.Analytics.Tracking;
using Lager.Android;
using ReactiveUI;
using System.Reactive.Threading.Tasks;
using Splat;
using Xamarin.InAppBilling;
using Exception = System.Exception;
using String = System.String;

namespace Espera.Android.Views
{
    [Activity]
    public class SettingsActivity : PreferenceActivity
    {
#if DEBUG
        private static readonly string PremiumId = ReservedTestProductIDs.Purchased;
#else
        private static readonly string PremiumId = "premium";
#endif

        private AndroidSettings androidSettings;
        private NotShittyInAppBillingHandler billingHandler;
        private UserSettings userSettings;

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            return AndroidVolumeRequests.Instance.HandleKeyCode(keyCode) || base.OnKeyDown(keyCode, e);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            // Xamarin.InAppBilling requires the codes to be passed to its handler
            if (this.billingHandler != null)
            {
                this.billingHandler.HandleActivityResult(requestCode, resultCode, data);
            }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.ActionBar.SetTitle(Resource.String.settings);

            this.userSettings = Locator.Current.GetService<UserSettings>();
            this.androidSettings = Locator.Current.GetService<AndroidSettings>();

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
            portPref.BindToSetting(this.userSettings, x => x.Port, x => x.Text, x => int.Parse(x.ToString()), x => x.ToString(), NetworkHelpers.IsPortValid);

            var ipAddressPref = (EditTextPreference)this.FindPreference(this.GetString(Resource.String.preference_ipaddress));
            ipAddressPref.EditText.InputType = InputTypes.ClassPhone;
            ipAddressPref.EditText.Events().TextChanged
                .Select(x => new string(x.Text.ToArray()))
                .Where(x => !IsValidIpAddress(x))
                .Subscribe(x =>
                {
                    ipAddressPref.EditText.Error = this.GetString(Resource.String.preference_ipaddress_validation_error);
                });
            ipAddressPref.BindToSetting(this.userSettings, x => x.ServerAddress, x => x.Text, x => (string)x, x => x, IsValidIpAddress);

            var saveEnergyPref = (SwitchPreference)this.FindPreference(this.GetString(Resource.String.preference_save_energy));
            saveEnergyPref.BindToSetting(this.androidSettings, x => x.SaveEnergy, x => x.Checked, x => bool.Parse(x.ToString()));

            var passwordPreference = (EditTextPreference)this.FindPreference(this.GetString(Resource.String.preference_administrator_password));
            passwordPreference.BindToSetting(this.userSettings, x => x.AdministratorPassword, x => x.Text, x => (string)x);
            this.userSettings.WhenAnyValue(x => x.IsPremium, x => x || TrialHelpers.IsInTrialPeriod(AppConstants.TrialTime))
                .BindTo(passwordPreference, x => x.Enabled);

            Preference premiumButton = this.FindPreference(this.GetString(Resource.String.preference_purchase_premium));
            premiumButton.Events().PreferenceClick.Select(_ => this.PurchasePremium().ToObservable()
                    .Catch<Unit, Exception>(ex => Observable.Start(() => this.TrackInAppPurchaseException(ex))))
                .Concat()
                .Subscribe();

            Preference restorePremiumButton = this.FindPreference(this.GetString(Resource.String.preference_restore_premium));
            restorePremiumButton.Events().PreferenceClick.Select(_ => this.RestorePremium().ToObservable()
                    .Catch<Unit, Exception>(ex => Observable.Start(() => this.TrackInAppPurchaseException(ex))))
                .Concat()
                .Subscribe();

            Preference versionPreference = this.FindPreference(this.GetString(Resource.String.preference_version));
            versionPreference.Summary = this.PackageManager.GetPackageInfo(this.PackageName, 0).VersionName;
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
            if (this.userSettings.IsPremium)
            {
                Toast.MakeText(this, Resource.String.premium_already_purchased, ToastLength.Long).Show();
                return;
            }

            this.billingHandler = new NotShittyInAppBillingHandler(this, "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAh32oezB4EXDKOOSHGgH+H4P9mgKdXqx5ji1ndAhdw9gvSp3uPthav07MZTlQPjRq62+0eUgddosWjgMedMAs7Ov4QeOsmKsR40SOpICGDM0JBDXA7OE9HeJdr+yTeyC4yf7OsTZi6YKf8nFI68VkejLqv9Ell36aK/MczlTy5yJJhmgYUcLaZndYeUg4AVEhF7dK40TvPu/F7wuxVDqRYcoT1loiMNvYIt+/Wi3N7UAU07Uav+apwOnQHfkcWwb9PgZcpKuF7R2U3yWECoRgwAaXHoFmtBy9FomQ4uBEJlWIlg7TTAuK8Y3Ytlgnf02uFS4W1j0QjkErriEEWjm5TwIDAQAB");

            try
            {
                await this.billingHandler.Connect().Timeout(TimeSpan.FromSeconds(30));
            }

            catch (TimeoutException)
            {
                Toast.MakeText(this, Resource.String.connection_timeout, ToastLength.Long).Show();
                return;
            }

            IReadOnlyList<Product> products = await this.billingHandler.QueryInventoryAsync(new List<string> { PremiumId }, ItemType.Product);
            Product premium = products.Single(x => x.ProductId == PremiumId);

            int billingResult = await this.billingHandler.BuyProduct(premium);

            if (billingResult == BillingResult.OK)
            {
                this.userSettings.IsPremium = true;
                Toast.MakeText(this, Resource.String.purchase_successful, ToastLength.Long).Show();
            }

            else
            {
                Toast.MakeText(this, Resource.String.purchase_failed, ToastLength.Long).Show();
            }

            await this.billingHandler.Disconnect();

            this.billingHandler = null;
        }

        private async Task RestorePremium()
        {
            if (this.userSettings.IsPremium)
            {
                Toast.MakeText(this, Resources.GetString(Resource.String.premium_already_purchased), ToastLength.Long).Show();
                return;
            }

            this.billingHandler = new NotShittyInAppBillingHandler(this, "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAh32oezB4EXDKOOSHGgH+H4P9mgKdXqx5ji1ndAhdw9gvSp3uPthav07MZTlQPjRq62+0eUgddosWjgMedMAs7Ov4QeOsmKsR40SOpICGDM0JBDXA7OE9HeJdr+yTeyC4yf7OsTZi6YKf8nFI68VkejLqv9Ell36aK/MczlTy5yJJhmgYUcLaZndYeUg4AVEhF7dK40TvPu/F7wuxVDqRYcoT1loiMNvYIt+/Wi3N7UAU07Uav+apwOnQHfkcWwb9PgZcpKuF7R2U3yWECoRgwAaXHoFmtBy9FomQ4uBEJlWIlg7TTAuK8Y3Ytlgnf02uFS4W1j0QjkErriEEWjm5TwIDAQAB");

            try
            {
                await this.billingHandler.Connect().Timeout(TimeSpan.FromSeconds(30));
            }

            catch (TimeoutException)
            {
                Toast.MakeText(this, Resources.GetString(Resource.String.connection_timeout), ToastLength.Long).Show();
                return;
            }

            IReadOnlyList<Purchase> products = this.billingHandler.GetPurchases(ItemType.Product);
            Purchase premium = products.SingleOrDefault(x => x.ProductId == PremiumId);

            if (premium == null || premium.PurchaseState != 0)
            {
                Toast.MakeText(this, Resource.String.purchase_restore_failed, ToastLength.Long).Show();
            }

            else
            {
                this.userSettings.IsPremium = true;
                Toast.MakeText(this, Resource.String.purchase_restored, ToastLength.Long).Show();
            }

            await this.billingHandler.Disconnect();

            this.billingHandler = null;
        }

        private void TrackInAppPurchaseException(Exception ex)
        {
            EasyTracker.GetInstance(this).Send(MapBuilder.CreateException(ex.StackTrace, Java.Lang.Boolean.False).Build());
        }
    }
}