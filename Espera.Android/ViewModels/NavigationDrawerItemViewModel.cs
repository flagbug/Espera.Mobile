using System.Collections.Specialized;
using ReactiveUI;

namespace Espera.Android.ViewModels
{
    public class NavigationDrawerItemViewModel : ReactiveObject
    {
        private NavigationDrawerItemViewModel()
        {
            this.IsEnabled = true;
        }

        public int? IconResourceId { get; private set; }

        public bool IsEnabled { get; set; }

        public MainDrawerItemType ItemType { get; private set; }

        public string Text { get; private set; }

        public static NavigationDrawerItemViewModel CreateDivider()
        {
            return new NavigationDrawerItemViewModel
            {
                ItemType = MainDrawerItemType.Divider,
                IsEnabled = false
            };
        }

        public static NavigationDrawerItemViewModel CreatePrimary(string text)
        {
            return new NavigationDrawerItemViewModel
            {
                Text = text,
                ItemType = MainDrawerItemType.Primary
            };
        }

        public static NavigationDrawerItemViewModel CreateSecondary(string text, int iconResourceId, bool isFirstSecondary = false)
        {
            return new NavigationDrawerItemViewModel
            {
                Text = text,
                IconResourceId = iconResourceId,
                ItemType = MainDrawerItemType.Secondary
            };
        }
    }
}