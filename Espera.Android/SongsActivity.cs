using Akavache;
using Android.App;
using Android.OS;
using Android.Widget;
using ReactiveUI;
using ReactiveUI.Mobile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Espera.Android
{
    [Activity(Label = "Songs")]
    public class SongsActivity : Activity
    {
        private readonly AutoSuspendActivityHelper autoSuspendHelper;

        public SongsActivity()
        {
            this.autoSuspendHelper = new AutoSuspendActivityHelper(this);
        }

        private ListView SongsListView
        {
            get { return this.FindViewById<ListView>(Resource.Id.songsList); }
        }

        protected override async void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            this.autoSuspendHelper.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Songs);

            string artist = this.Intent.GetStringExtra("artist");

            IReadOnlyList<Song> songs = await BlobCache.InMemory.GetObjectAsync<IReadOnlyList<Song>>("songs");

            songs = songs.Where(x => x.Artist.Equals(artist, StringComparison.OrdinalIgnoreCase))
               .ToList();

            var adapter = new SongsAdapter(this, songs);
            this.SongsListView.Adapter = adapter;
            this.SongsListView.ItemClick += async (sender, args) =>
            {
                await this.PlaySongs(GetSongGuidsFromAdapater(adapter, args.Position));
            };

            this.SongsListView.ItemLongClick += (sender, args) =>
            {
                var builder = new AlertDialog.Builder(this);
                builder.SetItems(new[] { "Play", "Add to playlist" }, async (o, eventArgs) =>
                {
                    switch (eventArgs.Which)
                    {
                        case 0:
                            await this.PlaySongs(GetSongGuidsFromAdapater(adapter, args.Position));
                            break;

                        case 1:
                            await this.AddToPlaylist(adapter[args.Position].Guid);
                            break;
                    }
                });
                builder.Create().Show();
            };
        }

        protected override void OnPause()
        {
            base.OnPause();
            this.autoSuspendHelper.OnPause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            this.autoSuspendHelper.OnResume();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            this.autoSuspendHelper.OnSaveInstanceState(outState);
        }

        private static IEnumerable<Guid> GetSongGuidsFromAdapater(SongsAdapter adapter, int start)
        {
            var guids = new List<Guid>();

            for (int i = start; i < adapter.Count; i++)
            {
                guids.Add(adapter[i].Guid);
            }

            return guids;
        }

        private async Task AddToPlaylist(Guid guid)
        {
            Tuple<int, string> response = await NetworkMessenger.Instance.AddSongToPlaylist(guid);

            string text = response.Item1 == 200 ? "Song added to playlist" : "Error adding song";

            Toast.MakeText(this, text, ToastLength.Short).Show();
        }

        private async Task PlaySongs(IEnumerable<Guid> guids)
        {
            Tuple<int, string> response = await NetworkMessenger.Instance.PlaySongs(guids);

            string text = response.Item1 == 200 ? "Playing songs" : "Error adding songs";

            Toast.MakeText(this, text, ToastLength.Short).Show();
        }
    }
}