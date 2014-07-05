using System;
using System.IO;
using System.Threading.Tasks;

namespace Espera.Mobile.Core.Network
{
    public interface ITcpClient : IDisposable
    {
        Task ConnectAsync(string ipAddress, int port);

        Stream GetStream();
    }
}