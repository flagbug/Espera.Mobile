using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Xamarin.InAppBilling;

namespace Espera.Android
{
    public class NotShittyInAppBillingHandler
    {
        private readonly InAppBillingServiceConnection serviceConnection;

        public NotShittyInAppBillingHandler(Activity activity, string publicKey)
        {
            this.serviceConnection = new InAppBillingServiceConnection(activity, publicKey);
        }

        public IObservable<int> BuyProduct(Product product)
        {
            return Observable.Create<int>(o =>
            {
                InAppBillingHandler.OnProductPurchaseCompletedDelegate purchaseCompleted = (response, purchase) => o.OnNext(response);

                this.serviceConnection.BillingHandler.OnProductPurchaseCompleted += purchaseCompleted;

                this.serviceConnection.BillingHandler.BuyProduct(product);

                return () => this.serviceConnection.BillingHandler.OnProductPurchaseCompleted -= purchaseCompleted;
            }).FirstAsync();
        }

        public IObservable<Unit> Connect()
        {
            return Observable.Create<Unit>(o =>
            {
                InAppBillingServiceConnection.OnConnectedDelegate connectedDelegate = () => o.OnNext(Unit.Default);

                this.serviceConnection.OnConnected += connectedDelegate;

                this.serviceConnection.Connect();

                return () => this.serviceConnection.OnConnected -= connectedDelegate;
            }).FirstAsync();
        }

        public IObservable<Unit> Disconnect()
        {
            return Observable.Create<Unit>(o =>
            {
                InAppBillingServiceConnection.OnDisconnectedDelegate disconnectedDelegate = () => o.OnNext(Unit.Default);

                this.serviceConnection.OnDisconnected += disconnectedDelegate;

                this.serviceConnection.Disconnect();

                return () => this.serviceConnection.OnDisconnected -= disconnectedDelegate;
            }).FirstAsync();
        }

        public void HandleActivityResult(int requestCode, Result resultCode, Intent data)
        {
            this.serviceConnection.BillingHandler.HandleActivityResult(requestCode, resultCode, data);
        }

        public async Task<IReadOnlyList<Product>> QueryInventoryAsync(IEnumerable<string> skuList, string itemType)
        {
            IList<Product> products = await this.serviceConnection.BillingHandler.QueryInventoryAsync(skuList.ToList(), itemType);

            return products.ToList();
        }
    }
}