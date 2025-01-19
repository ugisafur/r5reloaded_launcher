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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static launcher.ControlReferences;

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
            Utilities.HideDownloadOptlFiles();
            Ini.Set(Utilities.GetCurrentBranch().branch, "Download_HD_Textures", false);
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.IsInstalling)
            {
                Utilities.HideDownloadOptlFiles();
                return;
            }

            Task.Run(() => GameInstall.InstallOptionalFiles());
            Utilities.HideDownloadOptlFiles();
        }

        private void Later_Click(object sender, RoutedEventArgs e)
        {
            Utilities.HideDownloadOptlFiles();
            Ini.Set(Utilities.GetCurrentBranch().branch, "Download_HD_Textures", false);
        }
    }
}