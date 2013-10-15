using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Android;
using ReactiveUI.Mobile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Espera.Android
{
    [Activity(Label = "Songs", ConfigurationChanges = ConfigChanges.Orientation)]
    public class SongsActivity : ReactiveActivity<SongsViewModel>, IHandleDisconnect
    {
        private readonly AutoSuspendActivityHelper autoSuspendHelper;

        public SongsActivity()
        {
            this.autoSuspendHelper = new AutoSuspendActivityHelper(this);
        }

        private ListView SongsListView
        {
            get { return this.FindViewById<ListView>(Resource.Id.songsList); }
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            this.autoSuspendHelper.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Songs);

            IReadOnlyList<Song> songs = JsonConvert.DeserializeObject<IEnumerable<Song>>(this.Intent.GetStringExtra("songs")).ToList();
            this.ViewModel = new SongsViewModel(songs);

            this.OneWayBind(this.ViewModel, x => x.Songs, x => x.SongsListView.Adapter, x => new SongsAdapter(this, x));
            this.SongsListView.Events().ItemClick.Select(x => x.Position).InvokeCommand(this.ViewModel.PlaySongsCommand);

            this.SongsListView.ItemLongClick += (sender, args) =>
            {
                var builder = new AlertDialog.Builder(this);
                builder.SetItems(new[] { "Play", "Add to playlist" }, (o, eventArgs) =>
                {
                    switch (eventArgs.Which)
                    {
                        case 0:
                            this.ViewModel.PlaySongsCommand.Execute(args.Position);
                            break;

                        case 1:
                            this.ViewModel.AddToPlaylistCommand.Execute(args.Position);
                            break;
                    }
                });
                builder.Create().Show();
            };

            this.ViewModel.Message.Subscribe(x => Toast.MakeText(this, x, ToastLength.Short).Show());

            NetworkMessenger.Instance.Disconnected.Subscribe(x => this.HandleDisconnect());

            this.ViewModel.LoadArtistsCommand.Execute(null);
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
    }
}