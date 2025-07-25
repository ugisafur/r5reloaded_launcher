using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using launcher.GameLifecycle.Models;
using launcher.Game;
using launcher.Services;
using static launcher.Core.AppContext;
using static launcher.Core.AppController;

namespace launcher
{
    /// <summary>
    /// Interaction logic for InstallOptFilesPopup.xaml
    /// </summary>
    public partial class Popup_HD_Textures : UserControl
    {
        public Popup_HD_Textures()
        {
            InitializeComponent();
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            HideDownloadOptlFiles();
            ReleaseChannelService.SetDownloadHDTextures(false);
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            if (appState.IsInstalling)
            {
                HideDownloadOptlFiles();
                return;
            }

            Task.Run(() => GameInstaller.HDTextures());
            HideDownloadOptlFiles();
        }

        private void Later_Click(object sender, RoutedEventArgs e)
        {
            HideDownloadOptlFiles();
            ReleaseChannelService.SetDownloadHDTextures(false);
        }

        public void SetDownloadSize(GameManifest game)
        {
            
            long size = game.files.Sum(f => f.size);
            this.DownloadSize.Text = $"Download Size: {FormatBytesToGB(size)}";
        }

        private static string FormatBytesToGB(long bytes)
        {
            const double bytesInGB = 1024.0 * 1024.0 * 1024.0;
            double gigabytes = bytes / bytesInGB;
            return $"{gigabytes:F2} GB";
        }
    }
}