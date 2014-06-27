namespace Espera.Android.Views
{
    /*
    [Activity(ConfigurationChanges = ConfigChanges.Orientation)]
    public class RemoteSongsActivity : ReactiveActivity<RemoteSongsViewModel>
    {
        public ListView SongsList { get; private set; }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.RemoteSongs);
            this.WireUpControls();

            string songsJson = this.Intent.GetStringExtra("songs");
            var deserialized = JsonConvert.DeserializeObject<IEnumerable<NetworkSong>>(songsJson);
            var songs = new ReactiveList<NetworkSong>(deserialized);

            this.Title = songs.First().Artist;
            this.ViewModel = new RemoteSongsViewModel(songs);

            this.SongsList.Adapter = new RemoteSongsAdapter(this, this.ViewModel.Songs);
            this.SongsList.Events().ItemClick.Select(x => x.Position)
                .Subscribe(x =>
                {
                    if (UserSettings.Instance.DefaultLibraryAction == DefaultLibraryAction.PlayAll)
                    {
                        this.ViewModel.PlaySongsCommand.Execute(x);
                    }

                    else if (UserSettings.Instance.DefaultLibraryAction == DefaultLibraryAction.AddToPlaylist)
                    {
                        this.ViewModel.AddToPlaylistCommand.Execute(x);
                    }

                    else
                    {
                        throw new NotImplementedException();
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