using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            launcherVersionTxt.Text = Global.launcherVersion;
        }
    }
}