using launcher.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static launcher.Core.AppController;
using static launcher.Core.AppContext;

namespace launcher
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        private List<UserControl> pages = [];
        private List<Button> buttons = [];
        public GameSettings gameInstalls = new();

        public enum SettingsPage
        {
            Application = 0,
            Accessibility = 1,
            GameInstalls = 2,
            Download = 3,
            About = 4
        }

        public SettingsControl()
        {
            InitializeComponent();
        }

        private void SetSettingsTab(SettingsPage page)
        {
            // Get the currently visible page
            var visiblePage = GetVisiblePage();

            // Get the new page to display
            UserControl newPage = page switch
            {
                SettingsPage.Application => applicationPage,
                SettingsPage.Accessibility => accessibilityPage,
                SettingsPage.GameInstalls => gamePage,
                SettingsPage.Download => downloadPage,
                SettingsPage.About => aboutPage,
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
            foreach (var child in new UserControl[] { applicationPage, accessibilityPage, gamePage, downloadPage, aboutPage })
            {
                if (child.Visibility == Visibility.Visible)
                {
                    return child;
                }
            }

            return null;
        }

        public void SetupSettingsMenu()
        {
            pages.Add(applicationPage);
            pages.Add(accessibilityPage);
            pages.Add(gamePage);
            pages.Add(downloadPage);
            pages.Add(aboutPage);

            buttons.Add(ApplicationBtn);
            buttons.Add(AccessibilityBtn);
            buttons.Add(GameInstallsBtn);
            buttons.Add(DownloadBtn);
            buttons.Add(AboutBtn);

            SetSettingsTab(SettingsPage.Application);

            accessibilityPage.SetupAccessibilitySettings();
            applicationPage.SetupApplicationSettings();
            gamePage.SetupGameSettingsAsync();
            downloadPage.SetupDownloadSettings();
            aboutPage.SetupAboutSettings();

            gameInstalls = gamePage;
        }

        public void OpenDownloadsSettings()
        {
            SetSettingsTab(SettingsPage.Download);
            ShowSettingsControl();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (appState.InSettingsMenu)
                HideSettingsControl();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            int i = buttons.IndexOf(button);

            SetSettingsTab((SettingsPage)i);
        }
    }
}