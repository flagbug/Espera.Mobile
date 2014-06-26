using System.Net;

namespace Espera.Mobile.Core.Network
{
    public interface IWifiService
    {
        IPAddress GetIpAddress();
    }
}