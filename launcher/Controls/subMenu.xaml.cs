using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace launcher
{
    /// <summary>
    /// Interaction logic for subMenu.xaml
    /// </summary>
    public partial class subMenu : UserControl
    {
        public Button settingsButton;

        public subMenu()
        {
            InitializeComponent();
            settingsButton = Settings;
        }

        private void SupportCreator_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start https://ko-fi.com/amos0") { CreateNoWindow = true });
        }

        private void Discord_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start https://discord.com/invite/jqMkUdXrBr") { CreateNoWindow = true });
        }

        private void Website_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start https://r5reloaded.com") { CreateNoWindow = true });
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            ControlReferences.SubMenuPopup.IsOpen = false;
            Utilities.ShowSettingsControl();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void CreatorYoutube_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start https://www.youtube.com/channel/UCNIbjhwX7HrVCOtFZab_Jqg") { CreateNoWindow = true });
        }
    }
}