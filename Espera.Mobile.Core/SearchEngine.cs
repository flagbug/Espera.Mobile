using System;
using System.Collections.Generic;
using System.Linq;
using Espera.Network;

namespace Espera.Mobile.Core
{
    public static class SearchEngine
    {
        /// <summary>
        /// Filters the source by the specified search text.
        /// </summary>
        /// <param name="source">The songs to search.</param>
        /// <param name="searchText">
        /// The search text. A <c>null</c> or whitespace string simply returns the source sequence
        /// </param>
        /// <returns>The filtered sequence of songs.</returns>
        public static IEnumerable<T> FilterSongs<T>(this IEnumerable<T> source, string searchText) where T : NetworkSong
        {
            if (String.IsNullOrWhiteSpace(searchText))
                return source;

            IEnumerable<string> keyWords = searchText.Split(' ');

            return source
                .Where
                (
                    song => keyWords.All
                    (
                        keyword =>
                            song.Artist.ContainsIgnoreCase(keyword) ||
                            song.Album.ContainsIgnoreCase(keyword) ||
                            song.Genre.ContainsIgnoreCase(keyword) ||
                            song.Title.ContainsIgnoreCase(keyword)
                    )
                );
        }

        private static bool ContainsIgnoreCase(this string value, string other)
        {
            return value.IndexOf(other, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }
    }
}