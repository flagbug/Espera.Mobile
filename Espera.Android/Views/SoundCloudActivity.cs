using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core.ViewModels;
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

                var reactiveList = new ReactiveList<SoundCloudSongViewModel>();
                this.WhenAnyValue(x => x.ViewModel.Songs).Skip(1)
                    .Subscribe(x =>
                    {
                        using (reactiveList.SuppressChangeNotifications())
                        {
                            reactiveList.Clear();
                            reactiveList.AddRange(x);
                        }
                    }).DisposeWith(disposable);
                this.SoundCloudSongsList.Adapter = new ReactiveListAdapter<SoundCloudSongViewModel>(reactiveList, (vm, parent) => new SoundCloudSongView(this, vm, parent));
                this.SoundCloudSongsList.Events().ItemClick.Select(x => x.Position)
                    .Subscribe(this.DisplayAddToPlaylistDialog<SoundCloudViewModel, SoundCloudSongViewModel>)
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

                this.ViewModel.LoadCommand.ExecuteAsync()
                    .Subscribe(_ => this.SoundCloudSongsList.EmptyView = this.FindViewById(global::Android.Resource.Id.Empty))
                    .DisposeWith(disposable);

                return disposable;
            });
        }

        public ListView SoundCloudSongsList { get; private set; }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            base.OnCreateOptionsMenu(menu);

            this.MenuInflater.Inflate(Resource.Menu.options_menu, menu);

            var searchView = (SearchView)menu.FindItem(Resource.Id.search).ActionView;

            searchView.Events().QueryTextSubmit
                .SelectMany(async x =>
                {
                    this.ViewModel.SearchTerm = x.Query;
                    await this.ViewModel.LoadCommand.ExecuteAsync();
                    x.Handled = false;
                    searchView.ClearFocus();
                    return Unit.Default;
                }).Subscribe();

            return true;
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            return AndroidVolumeRequests.Instance.HandleKeyCode(keyCode) || base.OnKeyDown(keyCode, e);
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.SoundCloud);
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