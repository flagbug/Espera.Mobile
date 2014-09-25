using System;
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

        /// <summary>
        /// The action to invoke when this item is selected.
        /// </summary>
        public Action SelectionAction { get; private set; }

        public string Text { get; private set; }

        public static NavigationDrawerItemViewModel CreateDivider()
        {
            return new NavigationDrawerItemViewModel
            {
                ItemType = MainDrawerItemType.Divider,
                IsEnabled = false
            };
        }

        public static NavigationDrawerItemViewModel CreatePrimary(string text, Action selectionAction)
        {
            return new NavigationDrawerItemViewModel
            {
                Text = text,
                ItemType = MainDrawerItemType.Primary,
                SelectionAction = selectionAction
            };
        }

        public static NavigationDrawerItemViewModel CreateSecondary(string text, int iconResourceId, Action selectionAction)
        {
            return new NavigationDrawerItemViewModel
            {
                Text = text,
                IconResourceId = iconResourceId,
                ItemType = MainDrawerItemType.Secondary,
                SelectionAction = selectionAction
            };
        }
    }
}