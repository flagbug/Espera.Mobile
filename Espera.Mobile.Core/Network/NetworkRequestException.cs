using System;
using System.Collections;
using System.Collections.Generic;
using Espera.Network;

namespace Espera.Mobile.Core.Network
{
    /// <summary>
    /// The exception that is thrown when a request didn't succeed.
    ///
    /// This exception is different to a <see cref="NetworkException"/> in that it is thrown when the request didn't return
    /// <see cref="ResponseStatus.Success"/> and not when the network request itself fails, e.g because the connection has been closed.
    /// </summary>
    public class NetworkRequestException : Exception
    {
        public NetworkRequestException(RequestInfo requestInfo, ResponseInfo responseInfo)
            : base("Network request didn't succeed")
        {
            if (requestInfo == null)
                throw new ArgumentNullException("requestInfo");

            if (responseInfo == null)
                throw new ArgumentNullException("responseInfo");

            this.RequestInfo = requestInfo;
            this.ResponseInfo = responseInfo;
        }

        public override IDictionary Data
        {
            get
            {
                return new Dictionary<string, object>
                {
                    { "requestInfo", this.RequestInfo },
                    { "responseInfo", this.ResponseInfo }
                };
            }
        }

        public RequestInfo RequestInfo { get; private set; }

        public ResponseInfo ResponseInfo { get; private set; }
    }
}