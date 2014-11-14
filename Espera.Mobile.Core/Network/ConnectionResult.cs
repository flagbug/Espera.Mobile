using Espera.Network;
using System;

namespace Espera.Mobile.Core.Network
{
    public enum ConnectionResult
    {
        WrongPassword,
        Timeout,
        ServerVersionToLow,
        Successful,
        Failed,
        WifiDisabled
    }

    public class ConnectionResultContainer
    {
        public ConnectionResultContainer(ConnectionResult connectionResult, NetworkAccessPermission? accessPermission = null, Version serverVersion = null)
        {
            this.ConnectionResult = connectionResult;
            this.ServerVersion = serverVersion;
            this.AccessPermission = accessPermission;
        }

        public NetworkAccessPermission? AccessPermission { get; }

        public ConnectionResult ConnectionResult { get; }

        public Version ServerVersion { get; }
    }
}