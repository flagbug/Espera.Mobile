using System;
using System.IO;
using System.Threading.Tasks;

namespace Espera.Mobile.Core.Network
{
    public interface ITcpClient : IDisposable
    {
        /// <summary>
        /// Asynchronously connects to the host at the specified IP address and port.
        /// </summary>
        /// <param name="ipAddress">The IP address of the host.</param>
        /// <param name="port">The port to connect to.</param>
        /// <exception cref="NetworkException">
        /// Something went wrong while connecting to the host.
        /// </exception>
        Task ConnectAsync(string ipAddress, int port);

        Stream GetStream();
    }
}