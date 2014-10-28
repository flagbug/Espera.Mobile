using System;

namespace Espera.Mobile.Core
{
    public interface IInstallationDateFetcher
    {
        DateTimeOffset GetInstallationDate();
    }
}