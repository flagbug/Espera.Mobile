using System.Net;
using Android.App;
using Android.Net.Wifi;
using Espera.Mobile.Core.Network;

namespace Espera.Android
{
    internal class AndroidWifiService : IWifiService
    {
        public string GetIpAddress()
        {
            var wifiManager = WifiManager.FromContext(Application.Context);

            WifiInfo info = wifiManager.ConnectionInfo;

            return wifiManager.IsWifiEnabled ? new IPAddress(info.IpAddress).ToString() : null;
        }

        public int GetWifiSpeed()
        {
            var wifiManager = WifiManager.FromContext(Application.Context);

            WifiInfo info = wifiManager.ConnectionInfo;

            return info.LinkSpeed;
        }
    }
}