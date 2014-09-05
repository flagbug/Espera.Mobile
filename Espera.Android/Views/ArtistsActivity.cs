using System;
using System.Reactive.Linq;
using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core.Songs;
using Espera.Mobile.Core.ViewModels;
using ReactiveUI;

namespace Espera.Android.Views
{
    public abstract class ArtistsActivity<T> : ReactiveActivity<ArtistsViewModel<T>> where T : Song
    {
        private ProgressDialog progressDialog;

        public ListView ArtistList { get; private set; }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            return AndroidVolumeRequests.Instance.HandleKeyCode(keyCode) || base.OnKeyDown(keyCode, e);
        }

        protected abstract ArtistsViewModel<T> ConstructViewModel();

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Artists);
            this.WireUpControls();

            this.ViewModel = this.ConstructViewModel();

            this.OneWayBind(this.ViewModel, x => x.Artists, x => x.ArtistList.Adapter, list => new ArtistsAdapter(this, list));
            this.ArtistList.EmptyView = this.FindViewById(global::Android.Resource.Id.Empty);
            this.ArtistList.Events().ItemClick.Subscribe(x =>
            {
                this.ViewModel.SelectedArtist = (string)this.ArtistList.GetItemAtPosition(x.Position);
                this.OpenArtist();
            });

            this.progressDialog = new ProgressDialog(this);
            this.progressDialog.SetMessage(Resources.GetString(Resource.String.loading_artists));
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
                });

            this.ViewModel.LoadCommand.ThrownExceptions
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x => Toast.MakeText(this, Resource.String.loading_artists_failed, ToastLength.Long).Show());

            this.ViewModel.LoadCommand.Execute(null);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (this.progressDialog != null && this.progressDialog.IsShowing)
            {
                this.progressDialog.Dismiss();
            }
        }

        protected abstract void OpenArtist();
    }
}