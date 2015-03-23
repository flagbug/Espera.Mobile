using Espera.Mobile.Core;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;
using Windows.System.Profile;

namespace Espera.WinPhone.Platform
{
    internal class WinPhoneDeviceIdFactory : IDeviceIdFactory
    {
        public Guid GetDeviceId()
        {
            HardwareToken token = HardwareIdentification.GetPackageSpecificToken(null);
            IBuffer hardwareId = token.Id;

            HashAlgorithmProvider hasher = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5);
            IBuffer hashed = hasher.HashData(hardwareId);

            return new Guid(hashed.ToArray());
        }
    }
}