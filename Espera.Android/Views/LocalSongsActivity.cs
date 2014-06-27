namespace Espera.Android.Views
{
    /*
    [Activity(ConfigurationChanges = ConfigChanges.Orientation)]
    public class LocalSongsActivity : ReactiveActivity<LocalSongsViewModel>
    {
        public ListView SongsList { get; private set; }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

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
    }*/
}