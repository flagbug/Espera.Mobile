using Espera.Android.Network;
using System;

namespace Espera.Android
{
    public class ConnectionInfo
    {
        public ConnectionInfo(AccessPermission permission, Version serverVersion, ResponseInfo responseInfo)
        {
            if (responseInfo == null)
                throw new ArgumentNullException("responseInfo");

            this.AccessPermission = permission;
            this.ServerVersion = serverVersion;
            this.ResponseInfo = responseInfo;
        }

        public AccessPermission AccessPermission { get; private set; }

        public ResponseInfo ResponseInfo { get; private set; }

        public Version ServerVersion { get; private set; }
    }
}