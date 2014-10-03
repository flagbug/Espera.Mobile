using System;
using ReactiveUI;

namespace Espera.Mobile.Core.ViewModels
{
    public class NavigationItemViewModel : ReactiveObject
    {
        private NavigationItemViewModel()
        {
            this.IsEnabled = true;
        }

        public string IconSource { get; private set; }

        public bool IsDivider { get; private set; }

        public bool IsEnabled { get; set; }

        public bool IsPrimary { get; private set; }

        public bool IsSecondary { get; private set; }

        /// <summary>
        /// The action to invoke when this item is selected.
        /// </summary>
        public Action SelectionAction { get; private set; }

        public string Text { get; private set; }

        public static NavigationItemViewModel CreateDivider()
        {
            return new NavigationItemViewModel
            {
                IsDivider = true,
                IsEnabled = false
            };
        }

        public static NavigationItemViewModel CreatePrimary(string text, Action selectionAction)
        {
            return new NavigationItemViewModel
            {
                Text = text,
                IsPrimary = true,
                SelectionAction = selectionAction
            };
        }

        public static NavigationItemViewModel CreateSecondary(string text, string iconSource, Action selectionAction)
        {
            return new NavigationItemViewModel
            {
                Text = text,
                IconSource = iconSource,
                IsSecondary = true,
                SelectionAction = selectionAction
            };
        }
    }
}