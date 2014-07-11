namespace Espera.Mobile.Core.Network
{
    public interface IWifiService
    {
        string GetIpAddress();

        int GetWifiSpeed();
    }
}