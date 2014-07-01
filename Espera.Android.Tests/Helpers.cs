using Espera.Network;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Espera.Android.Tests
{
    public static class Helpers
    {
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