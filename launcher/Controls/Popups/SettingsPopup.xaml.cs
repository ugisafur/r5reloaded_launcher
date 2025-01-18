using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using static launcher.ControlReferences;

namespace launcher
{
    /// <summary>
    /// Interaction logic for SettingsPopup.xaml
    /// </summary>
    public partial class SettingsPopup : UserControl
    {
        public SettingsPopup()
        {
            InitializeComponent();
        }

        private void btnRepair_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => GameRepair.Start());
        }

        private void AdvancedOptions_Click(object sender, RoutedEventArgs e)
        {
            if (!AppState.InAdvancedMenu)
            {
                GameSettings_Popup.IsOpen = false;
                Utilities.ShowAdvancedControl();
            }
        }

        private void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => GameInstall.Uninstall());
        }

        private void OpenDir_Button_Click(object sender, RoutedEventArgs e)
        {
            if (Utilities.IsBranchInstalled() || Utilities.GetCurrentBranch().is_local_branch)
            {
                string dir = Utilities.GetBranchDirectory();

                if (Directory.Exists(dir))
                    System.Diagnostics.Process.Start("explorer.exe", dir);
            }
        }
    }
}