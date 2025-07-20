using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using launcher.Game;
using launcher.Global;

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
            Managers.App.HideDownloadOptlFiles();
            SetBranch.DownloadHDTextures(false);
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.IsInstalling)
            {
                Managers.App.HideDownloadOptlFiles();
                return;
            }

            Task.Run(() => Install.HDTextures());
            Managers.App.HideDownloadOptlFiles();
        }

        private void Later_Click(object sender, RoutedEventArgs e)
        {
            Managers.App.HideDownloadOptlFiles();
            SetBranch.DownloadHDTextures(false);
        }

        public void SetDownloadSize(GameFiles game)
        {
            
            long size = game.files.Sum(f => f.sizeInBytes);
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