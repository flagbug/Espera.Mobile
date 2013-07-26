using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Espera.Android
{
    internal class NetworkMessenger
    {
        public static async Task<IPEndPoint> DiscoverServer()
        {
            var client = new UdpClient(12345);

            UdpReceiveResult result = await client.ReceiveAsync();

            return result.RemoteEndPoint;
        }
    }
}