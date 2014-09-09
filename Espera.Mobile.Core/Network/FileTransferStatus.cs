using System;
using Espera.Network;

namespace Espera.Mobile.Core.Network
{
    public class FileTransferStatus
    {
        public FileTransferStatus(ResponseInfo responseInfo, IObservable<int> transferProgress = null)
        {
            if (responseInfo == null)
                throw new ArgumentNullException("responseInfo");

            if (transferProgress == null)
                throw new ArgumentNullException("transferProgress");

            this.ResponseInfo = responseInfo;
            this.TransferProgress = transferProgress;
        }

        public ResponseInfo ResponseInfo { get; private set; }

        public IObservable<int> TransferProgress { get; private set; }
    }
}