using System.IO;
using System.Windows;
using System.Windows.Controls;
using static launcher.Core.UiReferences;
using static launcher.Core.AppControllerService;
using launcher.GameManagement;
using launcher.Services;

namespace launcher
{
    public partial class Popup_Game_Menu : UserControl
    {
        public Popup_Game_Menu()
        {
            InitializeComponent();
        }

        private void btnRepair_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => GameRepairer.Start());
        }

        private void AdvancedOptions_Click(object sender, RoutedEventArgs e)
        {
            if (!Launcher.InAdvancedMenu)
            {
                GameSettings_Popup.IsOpen = false;
                ShowAdvancedControl();
            }
        }

        private void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => GameUninstaller.Start());
        }

        private void OpenDir_Button_Click(object sender, RoutedEventArgs e)
        {
            if (ReleaseChannelService.IsInstalled() || ReleaseChannelService.IsLocal() || Directory.Exists(ReleaseChannelService.GetDirectory()))
            {
                string dir = ReleaseChannelService.GetDirectory();

                if (Directory.Exists(dir))
                    System.Diagnostics.Process.Start("explorer.exe", dir);
            }
        }
    }
}