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
    public class YoutubeFragment : ReactiveFragment<YoutubeViewModel>
    {
        public YoutubeFragment()
        {
            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                var reactiveList = new ReactiveList<YoutubeSongViewModel>();
                this.WhenAnyValue(x => x.ViewModel.Songs).Skip(1)
                    .Subscribe(x =>
                    {
                        using (reactiveList.SuppressChangeNotifications())
                        {
                            reactiveList.Clear();
                            reactiveList.AddRange(x);
                        }
                    }).DisposeWith(disposable);
                this.YoutubeSongsList.Adapter = new ReactiveListAdapter<YoutubeSongViewModel>(reactiveList, (vm, parent) => new YoutubeSongView(this.Activity, vm, parent));

                this.YoutubeSongsList.Events().ItemClick
                    .Subscribe(x => this.DisplayAddToPlaylistDialog<YoutubeViewModel, YoutubeSongViewModel>(this.Activity, x.Position))
                    .DisposeWith(disposable);

                this.ViewModel.AddToPlaylistCommand.ThrownExceptions
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => Toast.MakeText(this.Activity, Resource.String.something_went_wrong, ToastLength.Short).Show())
                    .DisposeWith(disposable);

                var progressDialog = new ProgressDialog(this.Activity);
                progressDialog.SetMessage(Resources.GetString(Resource.String.loading_youtube));
                progressDialog.Indeterminate = true;
                progressDialog.SetCancelable(false);

                progressDialog.Show();

                this.ViewModel.LoadCommand.ExecuteAsync()
                    .Finally(progressDialog.Dismiss)
                    .Subscribe(_ => this.YoutubeSongsList.EmptyView = this.View.FindViewById(global::Android.Resource.Id.Empty))
                    .DisposeWith(disposable);

                return disposable;
            });
        }

        public ListView YoutubeSongsList { get; private set; }

        public override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetHasOptionsMenu(true);

            this.ViewModel = new YoutubeViewModel();
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.options_menu, menu);

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

            base.OnCreateOptionsMenu(menu, inflater);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.Youtube, null);

            this.WireUpControls(view);

            return view;
        }
    }
}