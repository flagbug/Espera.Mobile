using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Akavache;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
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

                var adapter = new ReactiveListAdapter<RemoteSongViewModel>(new ReactiveList<RemoteSongViewModel>(this.ViewModel.Songs),
                    (vm, parent) => new RemoteSongView(this, vm, parent));
                this.SongsList.Adapter = adapter;

                this.SongsList.Events().ItemClick.Select(x => x.Position)
                    .Subscribe(x =>
                    {
                        this.ViewModel.SelectedSong = this.ViewModel.Songs[x];

                        var items = new List<Tuple<string, IReactiveCommand>>();

                        if (this.ViewModel.IsAdmin)
                        {
                            items.Add(Tuple.Create(Resources.GetString(Resource.String.play), (IReactiveCommand)this.ViewModel.PlaySongsCommand));
                            items.Add(Tuple.Create(Resources.GetString(Resource.String.add_to_playlist), (IReactiveCommand)this.ViewModel.AddToPlaylistCommand));
                        }

                        else if (this.ViewModel.RemainingVotes > 0)
                        {
                            string voteString = string.Format(Resources.GetString(Resource.String.uses_vote), this.ViewModel.RemainingVotes);
                            items.Add(Tuple.Create(string.Format("{0} \n({1})", Resources.GetString(Resource.String.add_to_playlist), voteString),
                                (IReactiveCommand)this.ViewModel.AddToPlaylistCommand));
                        }

                        else
                        {
                            items.Add(Tuple.Create(Resources.GetString(Resource.String.no_votes_left), (IReactiveCommand)null));
                        }

                        var builder = new AlertDialog.Builder(this);
                        builder.SetItems(items.Select(y => y.Item1).ToArray(), (o, eventArgs) =>
                        {
                            IReactiveCommand command = items[eventArgs.Which].Item2;

                            if (command != null)
                            {
                                command.Execute(null);
                            }
                        });
                        builder.Create().Show();
                    })
                    .DisposeWith(disposable);

                Observable.Merge(this.ViewModel.PlaySongsCommand.Select(_ => Resource.String.playing_songs),
                        this.ViewModel.AddToPlaylistCommand.Select(_ => Resource.String.added_to_playlist),
                        this.ViewModel.PlaySongsCommand.ThrownExceptions.Merge(this.ViewModel.AddToPlaylistCommand.ThrownExceptions).Select(_ => Resource.String.error_adding_song))
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
    }
}