using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using ReactiveUI;
using ReactiveUI.Android;
using ReactiveUI.Mobile;
using System;
using System.Reactive.Linq;

namespace Espera.Android
{
    [Activity(Label = "Artists", ConfigurationChanges = ConfigChanges.Orientation)]
    public class ArtistsActivity : ReactiveActivity<ArtistsViewModel>
    {
        private readonly AutoSuspendActivityHelper autoSuspendHelper;
        private ProgressDialog progressDialog;

        public ArtistsActivity()
        {
            this.autoSuspendHelper = new AutoSuspendActivityHelper(this);
        }

        private ListView ArtistListView
        {
            get { return this.FindViewById<ListView>(Resource.Id.artistList); }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            this.autoSuspendHelper.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Artists);

            this.ViewModel = new ArtistsViewModel();

            this.OneWayBind(this.ViewModel, x => x.Artists, x => x.ArtistListView.Adapter, list => new ArtistsAdapter(this, list));
            this.ArtistListView.ItemClick += (sender, args) =>
                this.OpenArtist((string)this.ArtistListView.GetItemAtPosition(args.Position));

            this.progressDialog = new ProgressDialog(this);
            this.progressDialog.SetMessage("Loading artists");
            this.progressDialog.Indeterminate = true;
            this.progressDialog.SetCancelable(false);

            this.ViewModel.LoadCommand.IsExecuting.Skip(1)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    if (x)
                    {
                        this.progressDialog.Show();
                    }

                    else
                    {
                        this.progressDialog.Hide();
                    }
                });

            this.ViewModel.Messages.Subscribe(x => Toast.MakeText(this, x, ToastLength.Long).Show());

            this.ViewModel.LoadCommand.Execute(null);
        }

        protected override void OnPause()
        {
            base.OnPause();
            this.autoSuspendHelper.OnPause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            this.autoSuspendHelper.OnResume();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            this.autoSuspendHelper.OnSaveInstanceState(outState);
        }

        private void OpenArtist(string selectedArtist)
        {
            this.ViewModel.SelectedArtist = selectedArtist;

            var intent = new Intent(this, typeof(SongsActivity));
            intent.PutExtra("artist", selectedArtist);

            this.StartActivity(intent);
        }
    }
}