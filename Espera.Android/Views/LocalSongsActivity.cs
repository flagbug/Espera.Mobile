using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using Espera.Mobile.Core.Songs;
using Espera.Mobile.Core.ViewModels;
using Google.Analytics.Tracking;
using Newtonsoft.Json;
using ReactiveUI;
using ReactiveUI.Android;
using ReactiveUI.Mobile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Espera.Android.Views
{
    [Activity(ConfigurationChanges = ConfigChanges.Orientation)]
    public class LocalSongsActivity : ReactiveActivity<LocalSongsViewModel>
    {
        private readonly AutoSuspendActivityHelper autoSuspendHelper;

        public LocalSongsActivity()
        {
            this.autoSuspendHelper = new AutoSuspendActivityHelper(this);
        }

        public ListView SongsList { get; private set; }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            this.autoSuspendHelper.OnCreate(bundle);

            this.SetContentView(Resource.Layout.LocalSongs);
            this.WireUpControls();

            string songsJson = this.Intent.GetStringExtra("songs");
            var deserialized = JsonConvert.DeserializeObject<IEnumerable<LocalSong>>(songsJson);
            var songs = new ReactiveList<LocalSong>(deserialized);

            this.Title = songs.First().Artist;
            this.ViewModel = new LocalSongsViewModel(songs);

            var adapter = new ReactiveListAdapter<LocalSongViewModel>(this.ViewModel.Songs,
                (vm, parent) => new LocalSongView(this, vm, parent));
            this.SongsList.Adapter = adapter;

            this.SongsList.Events().ItemClick.Select(x => x.Position)
                .Subscribe(x => this.ViewModel.AddToPlaylistCommand.Execute(x));
            /*
            this.SongsList.Events().ItemLongClick.Select(x => x.Position)
                .Subscribe(x =>
                {
                    var builder = new AlertDialog.Builder(this);
                    builder.SetItems(new[] { "Play", "Add to playlist" }, (o, eventArgs) =>
                    {
                        switch (eventArgs.Which)
                        {
                            case 0:
                                this.ViewModel.PlaySongsCommand.Execute(x);
                                break;

                            case 1:
                                this.ViewModel.AddToPlaylistCommand.Execute(x);
                                break;
                        }
                    });
                    builder.Create().Show();
                });*/

            this.ViewModel.Message.Subscribe(x => Toast.MakeText(this, x, ToastLength.Short).Show());
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
    }
}