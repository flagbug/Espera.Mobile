using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core.ViewModels;
using Espera.Network;
using Google.Analytics.Tracking;
using ReactiveMarrow;
using ReactiveUI;

namespace Espera.Android.Views
{
    [Activity(Label = "SoundCloud")]
    public class SoundCloudActivity : ReactiveActivity<SoundCloudViewModel>
    {
        private ProgressDialog progressDialog;

        public SoundCloudActivity()
        {
            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                var reactiveList = new ReactiveList<NetworkSong>();
                this.WhenAnyValue(x => x.ViewModel.Songs).Skip(1)
                    .Subscribe(x =>
                    {
                        using (reactiveList.SuppressChangeNotifications())
                        {
                            reactiveList.Clear();
                            reactiveList.AddRange(x);
                        }
                    });

                this.SoundCloudSongsList.Adapter = new SoundCloudSongsAdapter(this, reactiveList);
                this.SoundCloudSongsList.Events().ItemClick.Select(x => x.Position)
                    .Subscribe(x =>
                    {
                        this.ViewModel.SelectedSong = this.ViewModel.Songs[x];

                        var items = new List<Tuple<string, IObservable<Unit>>>();

                        if (this.ViewModel.IsAdmin)
                        {
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

                this.ViewModel.AddToPlaylistCommand.ThrownExceptions
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => Toast.MakeText(this, Resource.String.something_went_wrong, ToastLength.Short).Show())
                    .DisposeWith(disposable);

                this.progressDialog = new ProgressDialog(this);
                this.progressDialog.SetMessage(Resources.GetString(Resource.String.loading_soundcloud));
                this.progressDialog.Indeterminate = true;
                this.progressDialog.SetCancelable(false);

                this.ViewModel.LoadCommand.IsExecuting
                    .Skip(1)
                    .Subscribe(x =>
                    {
                        if (x)
                        {
                            this.progressDialog.Show();
                        }

                        else if (this.progressDialog.IsShowing)
                        {
                            this.progressDialog.Dismiss();
                        }
                    }).DisposeWith(disposable);

                this.ViewModel.LoadCommand.Execute(null);

                return disposable;
            });
        }

        public ListView SoundCloudSongsList { get; private set; }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            return AndroidVolumeRequests.Instance.HandleKeyCode(keyCode) || base.OnKeyDown(keyCode, e);
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.SoundCloudSongs);
            this.WireUpControls();

            this.ViewModel = new SoundCloudViewModel();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (this.progressDialog != null && this.progressDialog.IsShowing)
            {
                this.progressDialog.Dismiss();
            }
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