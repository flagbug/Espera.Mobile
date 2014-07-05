using System;
using System.Threading.Tasks;

namespace Espera.Mobile.Core.Network
{
    public interface IUdpClient : IDisposable
    {
        void Initialize(string ipAddress, int port);

        Task<Tuple<byte[], string>> ReceiveAsync();
    }
}