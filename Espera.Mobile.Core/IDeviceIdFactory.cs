using System;

namespace Espera.Mobile.Core
{
    public interface IDeviceIdFactory
    {
        Guid GetDeviceId();
    }
}