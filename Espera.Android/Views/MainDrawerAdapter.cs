using Android.App;
using Android.Views;
using Android.Widget;
using Espera.Android.ViewModels;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Espera.Android.Views
{
    public class MainDrawerAdapter : BaseAdapter<NavigationDrawerItemViewModel>, IEnumerable<NavigationDrawerItemViewModel>
    {
        private readonly Activity context;
        private readonly List<NavigationDrawerItemViewModel> items;

        public MainDrawerAdapter(Activity context, IEnumerable<NavigationDrawerItemViewModel> items)
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

        public override NavigationDrawerItemViewModel this[int position]
        {
            get { return this.items[position]; }
        }

        public override bool AreAllItemsEnabled() => false;

        public IEnumerator<NavigationDrawerItemViewModel> GetEnumerator() => this.items.GetEnumerator();

        public override long GetItemId(int position) => position;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = null;
            NavigationDrawerItemViewModel viewModel = this[position];

            switch (viewModel.ItemType)
            {
                case MainDrawerItemType.Primary:
                    view = this.context.LayoutInflater.Inflate(Resource.Layout.MainDrawerPrimaryItem, null);
                    view.FindViewById<TextView>(Resource.Id.PrimaryItemText).Text = viewModel.Text;

                    float alpha = this.IsEnabled(position) ? 1.0f : 0.25f;
                    view.Alpha = alpha;
                    break;

                case MainDrawerItemType.Secondary:
                    view = this.context.LayoutInflater.Inflate(Resource.Layout.MainDrawerSecondaryItem, null);
                    view.FindViewById<ImageView>(Resource.Id.DetailImage).SetImageDrawable(this.context.Resources.GetDrawable(this[position].IconResourceId.Value));
                    view.FindViewById<TextView>(Resource.Id.DetailText).Text = viewModel.Text;
                    break;

                case MainDrawerItemType.Divider:
                    view = this.context.LayoutInflater.Inflate(Resource.Layout.MainDrawerDivider, null);
                    break;
            }

            return view;
        }

        public override bool IsEnabled(int position) => this.items[position].IsEnabled;

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}