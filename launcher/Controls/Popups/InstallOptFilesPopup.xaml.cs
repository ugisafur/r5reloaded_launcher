using launcher.Classes.BranchUtils;
using System.Windows;
using System.Windows.Controls;
using launcher.Classes.Global;
using launcher.Classes.Game;
using launcher.Classes.Utilities;
using launcher.Classes.Managers;

namespace launcher
{
    /// <summary>
    /// Interaction logic for InstallOptFilesPopup.xaml
    /// </summary>
    public partial class InstallOptFilesPopup : UserControl
    {
        public InstallOptFilesPopup()
        {
            InitializeComponent();
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            AppManager.HideDownloadOptlFiles();
            Ini.Set(GetBranch.Name(false), "Download_HD_Textures", false);
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.IsInstalling)
            {
                AppManager.HideDownloadOptlFiles();
                return;
            }

            Task.Run(() => Install.InstallOptionalFiles());
            AppManager.HideDownloadOptlFiles();
        }

        private void Later_Click(object sender, RoutedEventArgs e)
        {
            AppManager.HideDownloadOptlFiles();
            Ini.Set(GetBranch.Name(false), "Download_HD_Textures", false);
        }
    }
}