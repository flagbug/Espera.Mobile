using Espera.Network;
using System;

namespace Espera.Mobile.Core.Network
{
    public class FileTransferStatus
    {
        public FileTransferStatus(ResponseInfo responseInfo, IObservable<int> transferProgress)
        {
            if (responseInfo == null)
                throw new ArgumentNullException(nameof(responseInfo));

            if (transferProgress == null)
                throw new ArgumentNullException(nameof(transferProgress));

            this.ResponseInfo = responseInfo;
            this.TransferProgress = transferProgress;
        }

        public ResponseInfo ResponseInfo { get; }

        public IObservable<int> TransferProgress { get; }
    }
}