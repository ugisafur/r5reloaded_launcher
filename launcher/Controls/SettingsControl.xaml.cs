using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static launcher.Utilities;
using static launcher.Global;

namespace launcher
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        private List<UserControl> pages = new List<UserControl>();
        private List<Button> buttons = new List<Button>();

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
                SettingsPage.APPLICATION => applicationPage,
                SettingsPage.ACCESSIBILITY => accessibilityPage,
                SettingsPage.GAME_INSTALLS => gamePage,
                SettingsPage.DOWNLOAD => downloadPage,
                SettingsPage.ABOUT => aboutPage,
                _ => null,
            };

            double fadeSpeed = GetIniSetting(IniSettings.Disable_Transitions, false) ? 0 : 200;

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

            SetSettingsTab(SettingsPage.APPLICATION);

            accessibilityPage.SetupAccessibilitySettings();
            applicationPage.SetupApplicationSettings();
            //gamePage.SetupGameSettings();
            GameInstallsBtn.IsEnabled = false;
            downloadPage.SetupDownloadSettings();
            aboutPage.SetupAboutSettings();
        }

        public void OpenDownloadsSettings()
        {
            SetSettingsTab(SettingsPage.DOWNLOAD);
            Utilities.ShowSettingsControl();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (IN_SETTINGS_MENU)
                Utilities.HideSettingsControl();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            int i = buttons.IndexOf(button);

            SetSettingsTab((SettingsPage)i);
        }
    }
}