using System;

namespace Espera.Mobile.Core.Network
{
    /// <summary>
    /// The exception that is thrown when there was a problem with the underlying network connection.
    /// </summary>
    public class NetworkException : Exception
    {
        public NetworkException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public NetworkException(string message)
            : base(message)
        { }
    }
}