using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Views;
using Android.Widget;
using Espera.Mobile.Core.ViewModels;

namespace Espera.Android.Views
{
    public class MainDrawerAdapter : BaseAdapter<NavigationItemViewModel>, IEnumerable<NavigationItemViewModel>
    {
        private readonly Activity context;
        private readonly List<NavigationItemViewModel> items;

        public MainDrawerAdapter(Activity context, IEnumerable<NavigationItemViewModel> items)
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

        public override NavigationItemViewModel this[int position]
        {
            get { return this.items[position]; }
        }

        public override bool AreAllItemsEnabled()
        {
            return false;
        }

        public IEnumerator<NavigationItemViewModel> GetEnumerator()
        {
            return this.items.GetEnumerator();
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            throw new NotImplementedException();
        }

        /*
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = null;
            NavigationItemViewModel viewModel = this[position];

            switch (viewModel.ItemType)
            {
                case NavigationItemType.Primary:
                    view = this.context.LayoutInflater.Inflate(Resource.Layout.MainDrawerPrimaryItem, null);
                    view.FindViewById<TextView>(Resource.Id.PrimaryItemText).Text = viewModel.Text;

                    float alpha = this.IsEnabled(position) ? 1.0f : 0.25f;
                    view.Alpha = alpha;
                    break;

                case NavigationItemType.Secondary:
                    view = this.context.LayoutInflater.Inflate(Resource.Layout.MainDrawerSecondaryItem, null);
                    //view.FindViewById<ImageView>(Resource.Id.DetailImage).SetImageDrawable(this.context.Resources.GetDrawable(this[position].IconResourceId.Value));
                    view.FindViewById<TextView>(Resource.Id.DetailText).Text = viewModel.Text;
                    break;

                case NavigationItemType.Divider:
                    view = this.context.LayoutInflater.Inflate(Resource.Layout.MainDrawerDivider, null);
                    break;
            }

            return view;
        }*/

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public override bool IsEnabled(int position)
        {
            return this.items[position].IsEnabled;
        }
    }
}