using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using Espera.Android.ViewModels;
using Google.Analytics.Tracking;
using ReactiveUI;
using ReactiveUI.Android;
using ReactiveUI.Mobile;
using System;

namespace Espera.Android.Views
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

        public ListView ArtistList { get; private set; }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            this.autoSuspendHelper.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Artists);
            this.WireUpControls();

            this.ViewModel = new ArtistsViewModel();

            this.OneWayBind(this.ViewModel, x => x.Artists, x => x.ArtistList.Adapter, list => new ArtistsAdapter(this, list));
            this.ArtistList.Events().ItemClick.Subscribe(x => this.OpenArtist((string)this.ArtistList.GetItemAtPosition(x.Position)));

            this.progressDialog = new ProgressDialog(this);
            this.progressDialog.SetMessage("Loading artists");
            this.progressDialog.Indeterminate = true;
            this.progressDialog.SetCancelable(false);

            this.ViewModel.LoadCommand.IsExecuting
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

        private void OpenArtist(string artist)
        {
            var intent = new Intent(this, typeof(SongsActivity));
            intent.PutExtra("songs", this.ViewModel.SerializeSongsForSelectedArtist(artist));

            this.StartActivity(intent);
        }
    }
}