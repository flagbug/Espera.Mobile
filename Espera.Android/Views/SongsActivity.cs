using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Widget;
using Espera.Android.Settings;
using Espera.Android.ViewModels;
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
    [Activity(Label = "Songs", ConfigurationChanges = ConfigChanges.Orientation)]
    public class SongsActivity : ReactiveActivity<SongsViewModel>
    {
        private readonly AutoSuspendActivityHelper autoSuspendHelper;

        public SongsActivity()
        {
            this.autoSuspendHelper = new AutoSuspendActivityHelper(this);
        }

        public ListView SongsList { get; private set; }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            this.autoSuspendHelper.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Songs);
            this.WireUpControls();

            string songsJson = this.Intent.GetStringExtra("songs");
            IReadOnlyList<Song> songs = JsonConvert.DeserializeObject<IEnumerable<Song>>(songsJson).ToList();
            this.ViewModel = new SongsViewModel(songs);

            this.OneWayBind(this.ViewModel, x => x.Songs, x => x.SongsList.Adapter, x => new SongsAdapter(this, x));

            this.SongsList.Events().ItemClick.Select(x => x.Position)
                .Subscribe(x =>
                {
                    if (UserSettings.Instance.DefaultLibraryAction == DefaultLibraryAction.PlayAll)
                    {
                        this.ViewModel.PlaySongsCommand.Execute(x);
                    }

                    else
                    {
                        this.ViewModel.AddToPlaylistCommand.Execute(x);
                    }
                });

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
                });

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
    }
}