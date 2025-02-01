using launcher.Game;
using launcher.Managers;
using System.Windows;
using System.Windows.Controls;

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
            Managers.App.HideCheckExistingFiles();
        }

        private void CheckFiles_Click(object sender, RoutedEventArgs e)
        {
            Managers.App.HideCheckExistingFiles();
            Task.Run(() => Repair.Start());
        }
    }
}