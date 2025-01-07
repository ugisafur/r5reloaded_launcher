using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        private void SetSettingsTab(Global.SettingsPage page)
        {
            // Get the currently visible page
            var visiblePage = GetVisiblePage();

            // Get the new page to display
            UserControl newPage = page switch
            {
                Global.SettingsPage.Application => applicationPage,
                Global.SettingsPage.Accessibility => accessibilityPage,
                Global.SettingsPage.GameInstalls => gamePage,
                Global.SettingsPage.LaunchOptions => launchoptionsPage,
                Global.SettingsPage.Download => downloadPage,
                Global.SettingsPage.About => aboutPage,
                _ => null,
            };

            if (visiblePage != null && newPage != null && visiblePage != newPage)
            {
                // Fade out the old page
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
                fadeOut.Completed += (s, e) =>
                {
                    visiblePage.Visibility = Visibility.Hidden;

                    // Fade in the new page
                    newPage.Visibility = Visibility.Visible;
                    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                    newPage.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                };

                visiblePage.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }
            else if (newPage != null)
            {
                // If there is no currently visible page, just fade in the new page
                newPage.Visibility = Visibility.Visible;
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                newPage.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            }
        }

        private UserControl GetVisiblePage()
        {
            foreach (var child in new UserControl[] { applicationPage, accessibilityPage, gamePage, launchoptionsPage, downloadPage, aboutPage })
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
            pages.Add(launchoptionsPage);
            pages.Add(downloadPage);
            pages.Add(aboutPage);

            buttons.Add(ApplicationBtn);
            buttons.Add(AccessibilityBtn);
            buttons.Add(GameInstallsBtn);
            buttons.Add(LaunchOptionsBtn);
            buttons.Add(DownloadBtn);
            buttons.Add(AboutBtn);

            SetSettingsTab(Global.SettingsPage.Application);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Utilities.HideSettingsControl();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;

            int i = buttons.IndexOf(button);

            SetSettingsTab((Global.SettingsPage)i);
        }
    }
}