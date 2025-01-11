using System.Windows;
using System.Windows.Controls;
using static launcher.Global;
using static launcher.ControlReferences;

namespace launcher
{
    /// <summary>
    /// Interaction logic for DownloadsPopup.xaml
    /// </summary>
    public partial class DownloadsPopup : UserControl
    {
        private List<DownloadItem> downloadItems = new List<DownloadItem>();

        public DownloadsPopup()
        {
            InitializeComponent();
        }

        public DownloadItem AddDownloadItem(string fileName)
        {
            DownloadItem downloadItem = new DownloadItem();
            downloadItem.downloadFileName.Text = fileName;
            downloadItem.downloadFilePercent.Text = "0%";
            downloadItem.downloadFileProgress.Value = 0;
            downloadItems.Add(downloadItem);
            DownloadsStackPanel.Children.Add(downloadItem);
            ShowNoDownloadsText(downloadItems.Count == 0);
            return downloadItem;
        }

        public void RemoveDownloadItem(DownloadItem downloadItem)
        {
            downloadItems.Remove(downloadItem);
            DownloadsStackPanel.Children.Remove(downloadItem);
            ShowNoDownloadsText(downloadItems.Count == 0);
        }

        public void RemoveAllDownloadItems()
        {
            downloadItems.Clear();
            DownloadsStackPanel.Children.Clear();
            ShowNoDownloadsText(downloadItems.Count == 0);
        }

        public void ShowNoDownloadsText(bool show)
        {
            NoDownloadsLbl.Visibility = show ? Visibility.Visible : Visibility.Hidden;
        }

        private void gotoDownloads_Click(object sender, RoutedEventArgs e)
        {
            if (!IN_SETTINGS_MENU && !IN_ADVANCED_MENU)
            {
                mainApp.DownloadsPopup.IsOpen = false;
                settingsControl.OpenDownloadsSettings();
            }
        }
    }
}