using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Android.App;
using Espera.Mobile.Core.ViewModels;
using ReactiveMarrow;
using ReactiveUI;

namespace Espera.Android.Views
{
    public static class SongsActivityHelper
    {
        public static void DisplayAddToPlaylistDialog<T, TSong>(this ReactiveActivity<T> activity, int songPosition) where T : SongsViewModelBase<TSong>
        {
            activity.ViewModel.SelectedSong = activity.ViewModel.Songs[songPosition];

            var items = new List<Tuple<string, IObservable<Unit>>>();

            if (activity.ViewModel.IsAdmin)
            {
                items.Add(Tuple.Create(activity.Resources.GetString(Resource.String.add_to_playlist), activity.ViewModel.AddToPlaylistCommand.ExecuteAsync().ToUnit()));
            }

            else if (activity.ViewModel.RemainingVotes > 0)
            {
                string voteString = string.Format(activity.Resources.GetString(Resource.String.uses_vote), activity.ViewModel.RemainingVotes);
                items.Add(Tuple.Create(string.Format("{0} \n({1})", activity.Resources.GetString(Resource.String.add_to_playlist), voteString),
                    activity.ViewModel.AddToPlaylistCommand.ExecuteAsync().ToUnit()));
            }

            else
            {
                items.Add(Tuple.Create(activity.Resources.GetString(Resource.String.no_votes_left), Observable.Return(Unit.Default)));
            }

            var builder = new AlertDialog.Builder(activity);
            builder.SetItems(items.Select(y => y.Item1).ToArray(), async (o, eventArgs) =>
            {
                await items[eventArgs.Which].Item2;
            });
            builder.Create().Show();
        }
    }
}