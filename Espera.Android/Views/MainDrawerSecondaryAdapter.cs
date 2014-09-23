using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Views;
using Android.Widget;

namespace Espera.Android.Views
{
    /// <summary>
    /// <see cref="ListView"/> adapter for the secondary view in the main navigation drawer.
    ///
    /// Takes a <see cref="Tuple"/> that takes an image id as first type and string id as second type.
    /// </summary>
    internal class MainDrawerSecondaryAdapter : BaseAdapter<Tuple<int, int>>
    {
        private readonly Activity context;
        private readonly List<Tuple<int, int>> items;

        public MainDrawerSecondaryAdapter(Activity context, IEnumerable<Tuple<int, int>> items)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            if (items == null)
                throw new ArgumentNullException("items");

            this.context = context;
            this.items = items.ToList();
        }

        public override int Count
        {
            get { return this.items.Count; }
        }

        public override Tuple<int, int> this[int position]
        {
            get { return items[position]; }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView ?? this.context.LayoutInflater.Inflate(Resource.Layout.MainDrawerSecondaryItem, null);

            view.FindViewById<ImageView>(Resource.Id.DetailImage).SetImageDrawable(this.context.Resources.GetDrawable(this[position].Item1));
            view.FindViewById<TextView>(Resource.Id.DetailText).Text = this.context.Resources.GetString(this[position].Item2);

            return view;
        }
    }
}