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
using Espera.Network;
using ReactiveMarrow;
using ReactiveUI;

namespace Espera.Android.Views
{
    public abstract class ArtistsFragment<T> : ReactiveFragment<ArtistsViewModel<T>> where T : NetworkSong
    {
        protected ArtistsFragment()
        {
            this.WhenActivated(() =>
            {
                var disposable = new CompositeDisposable();

                this.OneWayBind(this.ViewModel, x => x.Artists, x => x.ArtistList.Adapter, list => new ArtistsAdapter(this.Activity, list))
                    .DisposeWith(disposable);
                this.ArtistList.FastScrollEnabled = true;

                this.ArtistList.Events().ItemClick.Subscribe(x =>
                {
                    this.ViewModel.SelectedArtist = (string)this.ArtistList.GetItemAtPosition(x.Position);
                    this.OpenArtist();
                }).DisposeWith(disposable);

                this.ViewModel.LoadCommand.IsExecuting
                    .Finally(() => this.Activity.SetProgressBarIndeterminateVisibility(false)) // Reset the visibility when the fragment closes
                    .Subscribe(this.Activity.SetProgressBarIndeterminateVisibility)
                    .DisposeWith(disposable);

                this.ViewModel.LoadCommand.ThrownExceptions
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(x => Toast.MakeText(this.Activity, Resource.String.loading_artists_failed, ToastLength.Long).Show())
                    .DisposeWith(disposable);

                this.ViewModel.LoadCommand.ExecuteAsync()
                    .SwallowNetworkExceptions()
                    .Subscribe(x => this.ArtistList.EmptyView = this.View.FindViewById(global::Android.Resource.Id.Empty))
                    .DisposeWith(disposable);

                return disposable;
            });
        }

        public ListView ArtistList { get; private set; }

        public override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.ViewModel = this.ConstructViewModel();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.Artists, null);

            this.WireUpControls(view);

            return view;
        }

        protected abstract ArtistsViewModel<T> ConstructViewModel();

        protected abstract void OpenArtist();
    }
}