using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Akavache;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
using Google.Analytics.Tracking;
using ReactiveMarrow;
using ReactiveUI;

namespace Espera.Android.Views
{
    [Activity]
    public class RemoteSongsActivity : ReactiveActivity<RemoteSongsViewModel>
    {
        public RemoteSongsActivity()
        {
            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                this.SongsList.Adapter = new RemoteSongsAdapter(this, new ReactiveList<NetworkSong>(this.ViewModel.Songs));
                this.SongsList.Events().ItemClick.Select(x => x.Position)
                    .Subscribe(x =>
                    {
                        this.ViewModel.SelectedSong = this.ViewModel.Songs[x];

                        var items = new List<Tuple<string, IObservable<Unit>>>();

                        if (this.ViewModel.IsAdmin)
                        {
                            items.Add(Tuple.Create(Resources.GetString(Resource.String.play), this.ViewModel.PlaySongsCommand.ExecuteAsync().ToUnit()));
                            items.Add(Tuple.Create(Resources.GetString(Resource.String.add_to_playlist), this.ViewModel.AddToPlaylistCommand.ExecuteAsync().ToUnit()));
                        }

                        else if (this.ViewModel.RemainingVotes > 0)
                        {
                            string voteString = string.Format(Resources.GetString(Resource.String.uses_vote), this.ViewModel.RemainingVotes);
                            items.Add(Tuple.Create(string.Format("{0} \n({1})", Resources.GetString(Resource.String.add_to_playlist), voteString),
                                this.ViewModel.AddToPlaylistCommand.ExecuteAsync().ToUnit()));
                        }

                        else
                        {
                            items.Add(Tuple.Create(Resources.GetString(Resource.String.no_votes_left), Observable.Return(Unit.Default)));
                        }

                        var builder = new AlertDialog.Builder(this);
                        builder.SetItems(items.Select(y => y.Item1).ToArray(), async (o, eventArgs) =>
                        {
                            await items[eventArgs.Which].Item2;
                        });
                        builder.Create().Show();
                    })
                    .DisposeWith(disposable);

                this.ViewModel.PlaySongsCommand.Select(x => x.Status == ResponseStatus.Success ? Resource.String.playing_songs : Resource.String.error_adding_songs)
                    .Merge(this.ViewModel.AddToPlaylistCommand.Select(x => x.Status == ResponseStatus.Success ? Resource.String.added_to_playlist : Resource.String.error_adding_song))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(x => Toast.MakeText(this, x, ToastLength.Short).Show())
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

            var songs = BlobCache.LocalMachine.GetObject<IEnumerable<NetworkSong>>(BlobCacheKeys.SelectedRemoteSongs).Wait().ToList();
            this.Title = songs.First().Artist;
            this.ViewModel = new RemoteSongsViewModel(songs);
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