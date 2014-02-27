using Android.Database;
using Android.Provider;
using Espera.Mobile.Core.SongFetchers;
using Espera.Network;
using System;
using System.Collections.Generic;

namespace Espera.Android
{
    public class AndroidSongFetcher : ISongFetcher
    {
        private readonly Func<string[], ICursor> query;

        public AndroidSongFetcher(Func<string[], ICursor> query)
        {
            this.query = query;
        }

        public IObservable<IReadOnlyList<NetworkSong>> GetSongsAsync()
        {
            string[] projection = {
                MediaStore.Audio.Media.InterfaceConsts.Id,
                MediaStore.Audio.Media.InterfaceConsts.Album,
                MediaStore.Audio.Media.InterfaceConsts.Artist,
                MediaStore.Audio.Media.InterfaceConsts.Duration,
                MediaStore.Audio.Media.InterfaceConsts.Title
            };

            ICursor cursor = query(projection);

            var list = new List<NetworkSong>();

            while (cursor.MoveToNext())
            {
                var song = new NetworkSong
                {
                    Album = cursor.GetString(1),
                    Artist = cursor.GetString(2),
                    Duration = TimeSpan.FromMilliseconds(Int32.Parse(cursor.GetString(3))),
                    Genre = string.Empty,
                    Source = NetworkSongSource.Local,
                    Title = cursor.GetString(4)
                };

                list.Add(song);
            }

            return System.Reactive.Linq.Observable.Return(list);
        }
    }
}