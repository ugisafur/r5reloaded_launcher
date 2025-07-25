using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using static launcher.Core.AppContext;
using static launcher.Core.AppController;
using launcher.Game;
using launcher.Services;

namespace launcher
{
    public partial class Popup_Install_Location : UserControl
    {
        public Popup_Install_Location()
        {
            InitializeComponent();
        }

        public void SetupInstallLocation()
        {
            if (string.IsNullOrEmpty((string)SettingsService.Get(SettingsService.Vars.Library_Location)))
            {
                DirectoryInfo parentDir = Directory.GetParent(Launcher.PATH.TrimEnd(Path.DirectorySeparatorChar));
                FolderLocation.Text = parentDir.FullName;
            }
            else
                FolderLocation.Text = (string)SettingsService.Get(SettingsService.Vars.Library_Location);
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            HideInstallLocation();
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty((string)SettingsService.Get(SettingsService.Vars.Library_Location)))
            {
                DirectoryInfo parentDir = Directory.GetParent(Launcher.PATH.TrimEnd(Path.DirectorySeparatorChar));
                SettingsService.Set(SettingsService.Vars.Library_Location, parentDir.FullName);
            }

            Directory.CreateDirectory(FolderLocation.Text);
            Task.Run(() => GameInstaller.Start());
            HideInstallLocation();
            Settings_Control.gamePage.SetLibraryPath(FolderLocation.Text);
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var directoryDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select Folder"
            };

            if (directoryDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                FolderLocation.Text = directoryDialog.FileName;
                SettingsService.Set(SettingsService.Vars.Library_Location, FolderLocation.Text);
            }
        }
    }
}