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
            launcherVersionTxt.Text = Constants.Launcher.VERSION;
        }
    }
}