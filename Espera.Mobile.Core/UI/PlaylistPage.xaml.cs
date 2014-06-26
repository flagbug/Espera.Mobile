using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Espera.Mobile.Core.ViewModels;
using ReactiveUI;
using Xamarin.Forms;

namespace Espera.Mobile.Core.UI
{
    public partial class PlaylistPage : ContentPage, IViewFor<PlaylistViewModel>
    {
        public static readonly BindableProperty ViewModelProperty =
            BindableProperty.Create<PlaylistPage, PlaylistViewModel>(x => x.ViewModel, null);

        public PlaylistPage()
        {
            InitializeComponent();

            this.ViewModel = new PlaylistViewModel();
            this.ViewModel.Activator.Activate();
            this.BindingContext = this.ViewModel;

            this.PreviousButton.GestureRecognizers.Add(new TapGestureRecognizer { Command = this.ViewModel.PlayPreviousSongCommand });
            this.PlayPauseButton.GestureRecognizers.Add(new TapGestureRecognizer { Command = this.ViewModel.PlayPauseCommand });
            this.NextButton.GestureRecognizers.Add(new TapGestureRecognizer { Command = this.ViewModel.PlayNextSongCommand });

            this.ViewModel.WhenAnyValue(x => x.IsPlaying)
                .Select(x => x ? "Pause.png" : "Play.png")
                .Select(ImageSource.FromFile) // We have to do this, because ReactiveUI thinks we are in WPF. We can remove this line as soon as this is fixed.
                .BindTo(this.PlayPauseButton, x => x.Source);

            Func<bool, double> opacitySelector = x => x ? 1 : 0.5;

            this.ViewModel.PlayPreviousSongCommand.CanExecuteObservable.Select(opacitySelector)
                .BindTo(this.PreviousButton, x => x.Opacity);

            this.ViewModel.PlayPauseCommand.CanExecuteObservable.Select(opacitySelector)
                .BindTo(this.PlayPauseButton, x => x.Opacity);

            this.ViewModel.PlayNextSongCommand.CanExecuteObservable.Select(opacitySelector)
                .BindTo(this.NextButton, x => x.Opacity);

            this.ViewModel.LoadPlaylistCommand.IsExecuting
                .BindTo(this.LoadIndicator, x => x.IsVisible);
            this.ViewModel.LoadPlaylistCommand.IsExecuting
                .BindTo(this.LoadIndicator, x => x.IsRunning);

            this.ViewModel.LoadPlaylistCommand.IsExecuting.CombineLatest(this.ViewModel.Entries.IsEmptyChanged,
                    (executing, empty) => !executing && empty)
                .BindTo(this.EmptyIndicator, x => x.IsVisible);

            this.ViewModel.LoadPlaylistCommand.IsExecuting
                .Select(x => !x)
                .BindTo(this.PlaylistContent, x => x.IsVisible);

            Observable.FromEventPattern<ItemTappedEventArgs>(h => this.PlaylistListView.ItemTapped += h, h => this.PlaylistListView.ItemTapped -= h)
                .Subscribe(async x =>
                {
                    bool hasVotesLeft = this.ViewModel.RemainingVotes > 0;
                    string voteString = hasVotesLeft ? string.Format("Vote ({0})", this.ViewModel.RemainingVotes) : "No votes left";

                    if (this.ViewModel.CanModify)
                    {
                        var actions = new List<string> { "Play", "Remove", "Move Up", "Move Down" };

                        if (this.ViewModel.CanVoteOnSelectedEntry)
                        {
                            actions.Add(voteString);
                        }

                        String action = await this.DisplayActionSheet("Playlist Actions", "Cancel", null, actions.ToArray());

                        if (action == actions[0])
                        {
                            await this.ViewModel.PlayPlaylistSongCommand.ExecuteAsync();
                        }

                        else if (action == actions[1])
                        {
                            await this.ViewModel.RemoveSongCommand.ExecuteAsync();
                        }

                        else if (action == actions[2])
                        {
                            await this.ViewModel.MoveSongUpCommand.ExecuteAsync();
                        }

                        else if (action == actions[3])
                        {
                            await this.ViewModel.MoveSongDownCommand.ExecuteAsync();
                        }

                        else if (hasVotesLeft && this.ViewModel.CanVoteOnSelectedEntry && action == actions[4])
                        {
                            await this.ViewModel.VoteCommand.ExecuteAsync();
                        }
                    }

                    else if (this.ViewModel.CanVoteOnSelectedEntry)
                    {
                        await this.DisplayActionSheet("Voting", "Cancel", null, voteString);

                        if (hasVotesLeft)
                        {
                            await this.ViewModel.VoteCommand.ExecuteAsync();
                        }
                    }
                });

            this.ViewModel.LoadPlaylistCommand.Execute(null);
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (PlaylistViewModel)value; }
        }

        public PlaylistViewModel ViewModel
        {
            get { return (PlaylistViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
    }
}