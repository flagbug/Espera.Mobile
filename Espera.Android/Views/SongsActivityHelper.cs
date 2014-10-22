using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Android.App;
using Android.Content;
using Espera.Mobile.Core.ViewModels;
using ReactiveMarrow;
using ReactiveUI;

namespace Espera.Android.Views
{
    public static class SongsActivityHelper
    {
        public static void DisplayAddToPlaylistDialog<T, TSong>(this IViewFor<T> activity, Context context, int songPosition) where T : SongsViewModelBase<TSong>
        {
            activity.ViewModel.SelectedSong = activity.ViewModel.Songs[songPosition];

            var items = new List<Tuple<string, IReactiveCommand>>();

            if (activity.ViewModel.IsAdmin)
            {
                items.Add(Tuple.Create(context.Resources.GetString(Resource.String.add_to_playlist), (IReactiveCommand)activity.ViewModel.AddToPlaylistCommand));
            }

            else if (activity.ViewModel.RemainingVotes > 0)
            {
                string voteString = string.Format(context.Resources.GetString(Resource.String.uses_vote), activity.ViewModel.RemainingVotes);
                items.Add(Tuple.Create(string.Format("{0} \n({1})", context.Resources.GetString(Resource.String.add_to_playlist), voteString),
                    (IReactiveCommand)activity.ViewModel.AddToPlaylistCommand));
            }

            else
            {
                items.Add(Tuple.Create(context.Resources.GetString(Resource.String.no_votes_left), (IReactiveCommand)null));
            }

            var builder = new AlertDialog.Builder(context);
            builder.SetItems(items.Select(y => y.Item1).ToArray(), (o, eventArgs) =>
            {
                IReactiveCommand command = items[eventArgs.Which].Item2;

                if (command != null)
                {
                    command.Execute(null);
                }
            });
            builder.Create().Show();
        }
    }
}