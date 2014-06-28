using System.Net;
using Android.App;
using Android.Content;
using Android.Net.Wifi;
using Espera.Mobile.Core.Network;

namespace Espera.Android
{
    internal class AndroidWifiService : IWifiService
    {
        public string GetIpAddress()
        {
            var wifiManager = (WifiManager)Application.Context.GetSystemService(Context.WifiService);

            WifiInfo info = wifiManager.ConnectionInfo;

            return new IPAddress(info.IpAddress).ToString();
        }
    }
}