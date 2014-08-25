using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Akavache;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core;
using Espera.Mobile.Core.Songs;
using Espera.Mobile.Core.ViewModels;
using Google.Analytics.Tracking;
using ReactiveMarrow;
using ReactiveUI;

namespace Espera.Android.Views
{
    [Activity(ConfigurationChanges = ConfigChanges.Orientation)]
    public class LocalSongsActivity : ReactiveActivity<LocalSongsViewModel>
    {
        public LocalSongsActivity()
        {
            var songs = BlobCache.InMemory.GetObject<IEnumerable<LocalSong>>(BlobCacheKeys.SelectedLocalSongs).Wait().ToList();
            this.ViewModel = new LocalSongsViewModel(songs);
            this.Title = songs.First().Artist;

            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                var adapter = new ReactiveListAdapter<LocalSongViewModel>(new ReactiveList<LocalSongViewModel>(this.ViewModel.Songs),
                    (vm, parent) => new LocalSongView(this, vm, parent));
                this.SongsList.Adapter = adapter;

                this.SongsList.Events().ItemClick.Select(x => x.Position)
                    .Subscribe(x =>
                    {
                        this.ViewModel.SelectedSong = this.ViewModel.Songs[x];

                        var builder = new AlertDialog.Builder(this);
                        builder.SetItems(new[] { "Add to playlist" }, async (o, eventArgs) =>
                        {
                            switch (eventArgs.Which)
                            {
                                case 0:
                                    await this.ViewModel.AddToPlaylistCommand.ExecuteAsync();
                                    break;
                            }
                        });

                        builder.Create().Show();
                    })
                    .DisposeWith(disposable);

                this.ViewModel.Messages.Subscribe(x => Toast.MakeText(this, x, ToastLength.Short).Show())
                    .DisposeWith(disposable);

                return disposable;
            });
        }

        public ListView SongsList { get; private set; }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            return AndroidVolumeRequests.Instance.HandleKeyCode(keyCode) || base.OnKeyDown(keyCode, e);
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.LocalSongs);
            this.WireUpControls();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            BlobCache.InMemory.InvalidateObject<IEnumerable<LocalSong>>(BlobCacheKeys.SelectedLocalSongs).Wait();
        }

        protected override void OnStart()
        {
            base.OnStart();

            EasyTracker.GetInstance(this).ActivityStart(this);
        }

        protected override void OnStop()
        {
            base.OnStop();

            EasyTracker.GetInstance(this).ActivityStop(this);
        }
    }
}