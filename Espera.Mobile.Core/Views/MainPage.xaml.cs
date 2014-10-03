using System.Collections.Generic;
using Espera.Mobile.Core.ViewModels;
using Xamarin.Forms;

namespace Espera.Mobile.Core.Views
{
    public partial class MainPage : MasterDetailPage
    {
        public MainPage()
        {
            InitializeComponent();

            this.FindByName<ListView>("NavigationListView").ItemsSource = CreateNavigationItems();
        }

        private IEnumerable<NavigationItemViewModel> CreateNavigationItems()
        {
            return new[]
            {
                NavigationItemViewModel.CreatePrimary(Properties.Resources.NavigationConnection, () => {}),
                NavigationItemViewModel.CreatePrimary(Properties.Resources.NavigationPlaylist, () => {}),
                NavigationItemViewModel.CreatePrimary(Properties.Resources.NavigationRemoteSongs, () => {}),
                NavigationItemViewModel.CreatePrimary(Properties.Resources.NavigationLocalSongs, () => {}),
                NavigationItemViewModel.CreatePrimary(Properties.Resources.NavigationSoundCloud, () => {}),
                NavigationItemViewModel.CreatePrimary(Properties.Resources.NavigationYoutube, () => {}),
                NavigationItemViewModel.CreateDivider(),
                NavigationItemViewModel.CreateSecondary(Properties.Resources.NavigationSettings, "Settings.png", () => {}),
                NavigationItemViewModel.CreateSecondary(Properties.Resources.NavigationFeedback, "Feedback.png", () => {})
            };
        }
    }
}