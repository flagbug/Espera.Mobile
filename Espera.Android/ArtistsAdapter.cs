using Android.App;
using Android.Views;
using Android.Widget;
using Java.Lang;
using System.Collections.Generic;
using System.Linq;

namespace Espera.Android
{
    internal class ArtistsAdapter : BaseAdapter<string>, ISectionIndexer
    {
        private readonly IReadOnlyList<string> artists;
        private readonly Activity context;
        private readonly Dictionary<string, int> sections;

        public ArtistsAdapter(Activity context, IReadOnlyList<string> artists)
        {
            this.context = context;
            this.artists = artists;

            this.sections = new Dictionary<string, int>();

            for (int i = 0; i < artists.Count; i++)
            {
                string key = artists[i][0].ToString();

                if (!this.sections.ContainsKey(key))
                {
                    this.sections.Add(key, i);
                }
            }
        }

        public override int Count
        {
            get { return artists.Count; }
        }

        public override string this[int position]
        {
            get { return artists[position]; }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public int GetPositionForSection(int section)
        {
            return this.sections[this.sections.Keys.ToList()[section]];
        }

        public int GetSectionForPosition(int position)
        {
            return this.sections[this.artists[position][0].ToString()];
        }

        public Object[] GetSections()
        {
            return this.sections.Keys.Select(x => (Object)new String(x)).ToArray();
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = convertView ?? context.LayoutInflater.Inflate(global::Android.Resource.Layout.SimpleListItem1, null);

            view.FindViewById<TextView>(global::Android.Resource.Id.Text1).Text = artists[position];

            return view;
        }
    }
}