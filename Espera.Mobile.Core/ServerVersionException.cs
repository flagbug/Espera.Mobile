using System;

namespace Espera.Mobile.Core
{
    public class ServerVersionException : Exception
    {
        public ServerVersionException(string message)
            : base(message)
        { }
    }
}