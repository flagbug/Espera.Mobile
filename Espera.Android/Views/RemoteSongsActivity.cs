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
    public class RemoteSongsActivity : ReactiveActivity<RemoteSongsViewModel>
    {
        public RemoteSongsActivity()
        {
            var songs = BlobCache.InMemory.GetObjectAsync<IEnumerable<RemoteSong>>(BlobCacheKeys.SelectedRemoteSongs).Wait().ToList();

            this.Title = songs.First().Artist;
            this.ViewModel = new RemoteSongsViewModel(songs);

            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                this.SongsList.Adapter = new RemoteSongsAdapter(this, new ReactiveList<RemoteSong>(this.ViewModel.Songs));
                this.SongsList.Events().ItemClick.Select(x => x.Position)
                    .Subscribe(x =>
                    {
                        this.ViewModel.SelectedSong = this.ViewModel.Songs[x];

                        var builder = new AlertDialog.Builder(this);
                        builder.SetItems(new[] { "Play", "Add to playlist" }, async (o, eventArgs) =>
                        {
                            switch (eventArgs.Which)
                            {
                                case 0:
                                    await this.ViewModel.PlaySongsCommand.ExecuteAsync();
                                    break;

                                case 1:
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

            this.SetContentView(Resource.Layout.RemoteSongs);
            this.WireUpControls();
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