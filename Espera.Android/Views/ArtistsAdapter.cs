using Android.App;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using Object = Java.Lang.Object;
using String = Java.Lang.String;

namespace Espera.Android.Views
{
    internal class ArtistsAdapter : BaseAdapter<string>, ISectionIndexer
    {
        private readonly IReadOnlyList<string> artists;
        private readonly Activity context;
        private readonly ILookup<string, int> sections;

        public ArtistsAdapter(Activity context, IReadOnlyList<string> artists)
        {
            this.context = context;
            this.artists = artists;

            this.sections = this.artists.Select((x, i) => Tuple.Create(x[0].ToString().ToUpper(), i))
                .ToLookup(x => x.Item1, x => x.Item2);
        }

        public override int Count
        {
            get { return artists.Count; }
        }

        public override bool HasStableIds
        {
            get { return true; }
        }

        public override string this[int position]
        {
            get { return artists[position]; }
        }

        public override long GetItemId(int position) => position;

        public int GetPositionForSection(int section) => this.sections.ElementAt(section).Min();

        public int GetSectionForPosition(int position)
        {
            return this.sections.Select((x, i) => x.Contains(position) ? i : -1)
                .First(x => x > -1);
        }

        public Object[] GetSections() => this.sections.Select(x => (Object)new String(x.Key)).ToArray();

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView ?? context.LayoutInflater.Inflate(global::Android.Resource.Layout.SimpleListItem1, null);

            view.FindViewById<TextView>(global::Android.Resource.Id.Text1).Text = artists[position];

            return view;
        }
    }
}