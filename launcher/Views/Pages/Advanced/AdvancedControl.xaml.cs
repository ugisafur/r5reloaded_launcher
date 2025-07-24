using launcher.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static launcher.Core.AppControllerService;
using static launcher.Core.UiReferences;

namespace launcher
{
    /// <summary>
    /// Interaction logic for AdvancedControl.xaml
    /// </summary>
    public partial class AdvancedControl : UserControl
    {
        private List<UserControl> pages = [];
        private List<Button> buttons = [];

        public enum AdvancedSettingsPage
        {
            General = 0,
            Graphics = 1,
            Network = 2,
            Server = 3,
            Performance = 4,
            Advanced = 5
        }

        public AdvancedControl()
        {
            InitializeComponent();
        }

        private void SetSettingsTab(AdvancedSettingsPage page)
        {
            // Get the currently visible page
            var visiblePage = GetVisiblePage();

            // Get the new page to display
            UserControl newPage = page switch
            {
                AdvancedSettingsPage.General => generalPage,
                AdvancedSettingsPage.Graphics => graphicsPage,
                AdvancedSettingsPage.Network => networkPage,
                AdvancedSettingsPage.Server => serverPage,
                AdvancedSettingsPage.Performance => performancePage,
                AdvancedSettingsPage.Advanced => advancedPage,
                _ => null,
            };

            double fadeSpeed = (bool)SettingsService.Get(SettingsService.Vars.Disable_Transitions) ? 1 : 200;

            if (visiblePage != null && newPage != null && visiblePage != newPage)
            {
                // Fade out the old page
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(fadeSpeed));
                fadeOut.Completed += (s, e) =>
                {
                    visiblePage.Visibility = Visibility.Hidden;
                    buttons[pages.IndexOf(visiblePage)].Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

                    // Fade in the new page
                    newPage.Visibility = Visibility.Visible;
                    buttons[pages.IndexOf(newPage)].Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));

                    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(fadeSpeed));
                    newPage.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                };

                visiblePage.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
            else if (newPage != null)
            {
                // If there is no currently visible page, just fade in the new page
                newPage.Visibility = Visibility.Visible;
                buttons[pages.IndexOf(newPage)].Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0));
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(fadeSpeed));
                newPage.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            }
        }

        private UserControl GetVisiblePage()
        {
            foreach (var child in new UserControl[] { generalPage, graphicsPage, networkPage, serverPage, performancePage, advancedPage })
            {
                if (child.Visibility == Visibility.Visible)
                {
                    return child;
                }
            }

            return null;
        }

        public void SetupAdvancedSettings()
        {
            pages.Add(generalPage);
            pages.Add(graphicsPage);
            pages.Add(networkPage);
            pages.Add(serverPage);
            pages.Add(performancePage);
            pages.Add(advancedPage);

            buttons.Add(GeneralBtn);
            buttons.Add(GraphicsBtn);
            buttons.Add(NetworkBtn);
            buttons.Add(ServerBtn);
            buttons.Add(PerformanceBtn);
            buttons.Add(AdvancedBtn);

            SetSettingsTab(AdvancedSettingsPage.General);

            generalPage.SetupGeneralSettings();
            graphicsPage.SetupGraphicsSettings();
            networkPage.SetupNetworkSettings();
            serverPage.SetupServerSettings();
            performancePage.SetupPerformanceSettings();
            advancedPage.SetupAdvancedSettings();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (appState.InAdvancedMenu)
                HideAdvancedControl();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            int i = buttons.IndexOf(button);

            SetSettingsTab((AdvancedSettingsPage)i);
        }
    }
}