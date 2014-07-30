using System;

namespace Espera.Mobile.Core.Network
{
    public class NetworkException : Exception
    {
        public NetworkException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}