using launcher.Core;
using System.Diagnostics;
using System.Windows.Controls;

namespace launcher
{
    /// <summary>
    /// Interaction logic for AboutSettings.xaml
    /// </summary>
    public partial class AboutSettings : UserControl
    {
        public AboutSettings()
        {
            InitializeComponent();
        }

        public void SetupAboutSettings()
        {
            // Set the initial state of the toggle switches
            launcherVersionTxt.Text = Launcher.VERSION;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {e.Uri.AbsoluteUri}") { CreateNoWindow = true });
            e.Handled = true;
        }
    }
}