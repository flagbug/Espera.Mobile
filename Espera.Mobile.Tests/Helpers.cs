using Espera.Network;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Espera.Mobile.Core;
using Espera.Mobile.Core.Settings;
using Espera.Mobile.Core.ViewModels;
using NSubstitute;

namespace Espera.Android.Tests
{
    public static class Helpers
    {
        public static ConnectionViewModel CreateDefaultConnectionViewModel(UserSettings settings = null, string localIpAddress = "192.168.1.2")
        {
            var installationDateFetcher = Substitute.For<IInstallationDateFetcher>();
            installationDateFetcher.GetInstallationDate().Returns(DateTimeOffset.MinValue);
            var clock = Substitute.For<IClock>();
            clock.Now.Returns(DateTimeOffset.MinValue);

            return new ConnectionViewModel(settings ?? new UserSettings(), () => localIpAddress, installationDateFetcher, clock);
        }

        public static NetworkSong SetupSong()
        {
            return new NetworkSong
            {
                Album = String.Empty,
                Artist = String.Empty,
                Duration = TimeSpan.Zero,
                Genre = String.Empty,
                Guid = Guid.NewGuid(),
                Source = NetworkSongSource.Local,
                Title = String.Empty
            };
        }

        public static ReadOnlyCollection<NetworkSong> SetupSongs(int count)
        {
            return Enumerable.Range(0, count).Select(x => SetupSong())
                .ToList().AsReadOnly();
        }

        public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> list)
        {
            return list.ToList();
        }

        public static Task<T> ToTaskResult<T>(this T value)
        {
            return Task.FromResult(value);
        }
    }
}