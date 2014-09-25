using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;

namespace Espera.Android
{
    public class MainDrawerPrimaryAdapter : BaseAdapter<string>
    {
        private readonly Activity context;
        private readonly Dictionary<int, bool> enabledState;
        private readonly List<string> items;

        public MainDrawerPrimaryAdapter(Activity context, IEnumerable<string> items)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            if (items == null)
                throw new ArgumentNullException("items");

            this.context = context;
            this.items = items.ToList();

            this.enabledState = new Dictionary<int, bool>();
        }

        public override int Count
        {
            get { return this.items.Count; }
        }

        public override string this[int position]
        {
            get { return this.items[position]; }
        }

        public override bool AreAllItemsEnabled()
        {
            return false;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView ?? this.context.LayoutInflater.Inflate(Resource.Layout.MainDrawerPrimaryItem, null);

            view.FindViewById<TextView>(Resource.Id.PrimaryItemText).Text = this[position];

            float alpha = this.IsEnabled(position) ? 1.0f : 0.25f;
            view.Alpha = alpha;

            return view;
        }

        public override bool IsEnabled(int position)
        {
            bool isEnabled;

            if (this.enabledState.TryGetValue(position, out isEnabled))
            {
                return isEnabled;
            }

            this.enabledState[position] = true;

            return true;
        }

        public void SetIsEnabled(int position, bool isEnabled)
        {
            this.enabledState[position] = isEnabled;

            this.NotifyDataSetChanged();
        }
    }
}