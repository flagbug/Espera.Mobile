using System;
using Espera.Network;

namespace Espera.Mobile.Core.Network
{
    public enum ConnectionResult
    {
        WrongPassword,
        Timeout,
        ServerVersionToLow,
        Successful,
        Failed
    }

    public class ConnectionResultContainer
    {
        public ConnectionResultContainer(ConnectionResult connectionResult, NetworkAccessPermission? accessPermission = null, Version serverVersion = null)
        {
            this.ConnectionResult = connectionResult;
            this.ServerVersion = serverVersion;
            this.AccessPermission = accessPermission;
        }

        public NetworkAccessPermission? AccessPermission { get; private set; }

        public ConnectionResult ConnectionResult { get; private set; }

        public Version ServerVersion { get; private set; }
    }
}