using launcher.Game;
using System.Windows;
using System.Windows.Controls;
using static launcher.Core.AppController;

namespace launcher
{
    /// <summary>
    /// Interaction logic for InstallOptFilesPopup.xaml
    /// </summary>
    public partial class Popup_Existing_Files : UserControl
    {
        public Popup_Existing_Files()
        {
            InitializeComponent();
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            HideCheckExistingFiles();
        }

        private void CheckFiles_Click(object sender, RoutedEventArgs e)
        {
            HideCheckExistingFiles();
            Task.Run(() => GameRepairer.Start());
        }
    }
}