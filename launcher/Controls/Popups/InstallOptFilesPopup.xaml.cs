using System.Windows;
using System.Windows.Controls;
using launcher.Utilities;
using launcher.Game;
using launcher.Global;
using launcher.Managers;
using launcher.BranchUtils;

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
            SetBranch.DownloadHDTextures(false);
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.IsInstalling)
            {
                AppManager.HideDownloadOptlFiles();
                return;
            }

            Task.Run(() => Install.HDTextures());
            AppManager.HideDownloadOptlFiles();
        }

        private void Later_Click(object sender, RoutedEventArgs e)
        {
            AppManager.HideDownloadOptlFiles();
            SetBranch.DownloadHDTextures(false);
        }
    }
}