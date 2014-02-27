using System;

namespace Espera.Mobile.Core.Network
{
    public class FileTransferStatus
    {
        public FileTransferStatus(IObservable<int> transferProgress)
        {
            if (transferProgress == null)
                throw new ArgumentNullException("transferProgress");

            this.TransferProgress = transferProgress;
        }

        public IObservable<int> TransferProgress { get; private set; }
    }
}