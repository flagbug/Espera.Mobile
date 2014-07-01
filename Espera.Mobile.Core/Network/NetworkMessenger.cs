using System;
using Splat;

namespace Espera.Mobile.Core.Network
{
    public static class NetworkMessenger
    {
        private static Lazy<INetworkMessenger> instance;

        static NetworkMessenger()
        {
            instance = new Lazy<INetworkMessenger>(() => Locator.Current.GetService<INetworkMessenger>());
        }

        public static INetworkMessenger Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        /// Overrides the instance for unit testing.
        /// </summary>
        public static void Override(INetworkMessenger messenger)
        {
            instance = new Lazy<INetworkMessenger>(() => messenger);
        }
    }
}