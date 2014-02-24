using Espera.Mobile.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Espera.Android.Tests
{
    public static class Helpers
    {
        public static IReadOnlyList<Song> SetupSongs(int count)
        {
            return Enumerable.Range(0, count).Select(x =>
                    new Song(String.Empty, String.Empty, String.Empty, String.Empty, TimeSpan.Zero, Guid.NewGuid(), SongSource.Local))
                .ToList();
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