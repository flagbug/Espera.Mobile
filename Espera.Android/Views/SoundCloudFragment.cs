using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Android.OS;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core;
using Espera.Mobile.Core.ViewModels;
using ReactiveMarrow;
using ReactiveUI;

namespace Espera.Android.Views
{
    public class SoundCloudFragment : ReactiveFragment<SoundCloudViewModel>
    {
        public SoundCloudFragment()
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
                this.SoundCloudSongsList.Adapter = new ReactiveListAdapter<SoundCloudSongViewModel>(reactiveList, (vm, parent) => new SoundCloudSongView(this.Activity, vm, parent));
                this.SoundCloudSongsList.Events().ItemClick
                    .Subscribe(x => this.DisplayAddToPlaylistDialog<SoundCloudViewModel, SoundCloudSongViewModel>(this.Activity, x.Position))
                    .DisposeWith(disposable);

                this.ViewModel.LoadCommand.ThrownExceptions.Merge(this.ViewModel.AddToPlaylistCommand.ThrownExceptions)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => Toast.MakeText(this.Activity, Resource.String.something_went_wrong, ToastLength.Short).Show())
                    .DisposeWith(disposable);

                this.ViewModel.LoadCommand.IsExecuting
                    .Subscribe(x => this.ProgressSpinner.Visibility = x ? ViewStates.Visible : ViewStates.Gone)
                    .DisposeWith(disposable);

                this.ViewModel.LoadCommand.ExecuteAsync()
                    .SwallowNetworkExceptions()
                    .Subscribe(_ => this.SoundCloudSongsList.EmptyView = this.View.FindViewById(global::Android.Resource.Id.Empty))
                    .DisposeWith(disposable);

                return disposable;
            });
        }

        public ProgressBar ProgressSpinner { get; private set; }

        public ListView SoundCloudSongsList { get; private set; }

        public override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetHasOptionsMenu(true);

            this.ViewModel = new SoundCloudViewModel();
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.SearchMenu, menu);

            var searchView = (SearchView)menu.FindItem(Resource.Id.search).ActionView;

            searchView.Events().QueryTextSubmit
                .SelectMany(async x =>
                {
                    this.ViewModel.SearchTerm = x.Query;

                    await this.ViewModel.LoadCommand.ExecuteAsync().SwallowNetworkExceptions();

                    x.Handled = false;
                    searchView.ClearFocus();

                    this.SoundCloudSongsList.SetSelectionAfterHeaderView();

                    return Unit.Default;
                }).Subscribe();

            base.OnCreateOptionsMenu(menu, inflater);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.SoundCloud, null);

            this.WireUpControls(view);

            return view;
        }

        public override void OnResume()
        {
            base.OnResume();

            this.Activity.Title = "SoundCloud";
        }
    }
}