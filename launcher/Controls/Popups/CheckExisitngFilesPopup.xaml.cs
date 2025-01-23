using launcher.Classes.Game;
using launcher.Classes.Managers;
using System.Windows;
using System.Windows.Controls;

namespace launcher
{
    /// <summary>
    /// Interaction logic for InstallOptFilesPopup.xaml
    /// </summary>
    public partial class CheckExisitngFilesPopup : UserControl
    {
        public CheckExisitngFilesPopup()
        {
            InitializeComponent();
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            AppManager.HideCheckExistingFiles();
        }

        private void CheckFiles_Click(object sender, RoutedEventArgs e)
        {
            AppManager.HideCheckExistingFiles();
            Task.Run(() => Repair.Start());
        }
    }
}