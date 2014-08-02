using System;

namespace Espera.Mobile.Core
{
    public class WrongPasswordException : Exception
    {
        public WrongPasswordException(string message)
            : base(message)
        { }
    }
}