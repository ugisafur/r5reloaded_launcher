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
            if (!Global.inSettingsMenu && !Global.inAdvancedMenu)
            {
                ControlReferences.App.DownloadsPopup.IsOpen = false;
                ControlReferences.settingsControl.OpenDownloadsSettings();
            }
        }
    }
}