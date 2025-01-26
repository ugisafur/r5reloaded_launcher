using launcher.Classes.BranchUtils;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using launcher.Classes.Global;
using launcher.Classes.Game;
using static launcher.Classes.Global.References;
using launcher.Classes.Managers;

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
            Task.Run(() => Repair.Start());
        }

        private void AdvancedOptions_Click(object sender, RoutedEventArgs e)
        {
            if (!AppState.InAdvancedMenu)
            {
                GameSettings_Popup.IsOpen = false;
                AppManager.ShowAdvancedControl();
            }
        }

        private void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => Uninstall.Start());
        }

        private void OpenDir_Button_Click(object sender, RoutedEventArgs e)
        {
            if (GetBranch.Installed() || GetBranch.IsLocalBranch() || Directory.Exists(GetBranch.Directory()))
            {
                string dir = GetBranch.Directory();

                if (Directory.Exists(dir))
                    System.Diagnostics.Process.Start("explorer.exe", dir);
            }
        }
    }
}