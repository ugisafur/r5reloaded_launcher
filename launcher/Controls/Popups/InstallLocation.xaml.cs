using launcher.Classes.BranchUtils;
using System.Windows;
using System.Windows.Controls;
using launcher.Classes.Global;
using launcher.Classes.Game;
using launcher.Classes.Utilities;
using launcher.Classes.Managers;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using static launcher.Classes.Global.References;

namespace launcher
{
    public partial class InstallLocation : UserControl
    {
        public InstallLocation()
        {
            InitializeComponent();
        }

        public void SetupInstallLocation()
        {
            if (string.IsNullOrEmpty((string)Ini.Get(Ini.Vars.Library_Location)))
            {
                DirectoryInfo parentDir = Directory.GetParent(Launcher.PATH.TrimEnd(Path.DirectorySeparatorChar));
                FolderLocation.Text = parentDir.FullName;
            }
            else
                FolderLocation.Text = (string)Ini.Get(Ini.Vars.Library_Location);
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            AppManager.HideInstallLocation();
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty((string)Ini.Get(Ini.Vars.Library_Location)))
            {
                DirectoryInfo parentDir = Directory.GetParent(Launcher.PATH.TrimEnd(Path.DirectorySeparatorChar));
                Ini.Set(Ini.Vars.Library_Location, parentDir.FullName);
            }

            Directory.CreateDirectory(FolderLocation.Text);
            Task.Run(() => Install.Start());
            AppManager.HideInstallLocation();
            Settings_Control.gamePage.SetLibaryPath(FolderLocation.Text);
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
                Ini.Set(Ini.Vars.Library_Location, FolderLocation.Text);
            }
        }
    }
}