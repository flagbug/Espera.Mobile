using System;
using System.Collections.Generic;
using Android.Database;
using Android.Provider;
using Espera.Mobile.Core.SongFetchers;
using Espera.Mobile.Core.Songs;

namespace Espera.Android
{
    public class AndroidSongFetcher : ISongFetcher<LocalSong>
    {
        private readonly Func<string[], ICursor> query;

        public AndroidSongFetcher(Func<string[], ICursor> query)
        {
            this.query = query;
        }

        public IObservable<IReadOnlyList<LocalSong>> GetSongsAsync()
        {
            string[] projection = {
                MediaStore.Audio.Media.InterfaceConsts.Title,
                MediaStore.Audio.Media.InterfaceConsts.Artist,
                MediaStore.Audio.Media.InterfaceConsts.Album,
                MediaStore.Audio.Genres.InterfaceConsts.Name,
                MediaStore.Audio.Media.InterfaceConsts.Duration,
                MediaStore.Audio.Media.InterfaceConsts.Data
            };

            var list = new List<LocalSong>();

            using (ICursor cursor = query(projection))
            {
                while (cursor.MoveToNext())
                {
                    var song = new LocalSong(cursor.GetString(0), cursor.GetString(1), cursor.GetString(2),
                        cursor.GetString(3), TimeSpan.FromMilliseconds(cursor.GetInt(4)), cursor.GetString(5));

                    list.Add(song);
                }
            }

            return System.Reactive.Linq.Observable.Return(list);
        }
    }
}