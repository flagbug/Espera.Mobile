using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Android.Widget;

namespace Espera.Android
{
    /// <summary>
    /// A list adapter that has deactivatable items.
    /// </summary>
    public class DeactivatableListAdapter<T> : ArrayAdapter<T>
    {
        private readonly Dictionary<int, bool> enabledState;

        public DeactivatableListAdapter(Context context, int textViewResourceId, T[] objects)
            : base(context, textViewResourceId, objects)
        {
            this.enabledState = new Dictionary<int, bool>();
        }

        public override bool AreAllItemsEnabled()
        {
            return false;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            View view = base.GetView(position, convertView, parent);

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