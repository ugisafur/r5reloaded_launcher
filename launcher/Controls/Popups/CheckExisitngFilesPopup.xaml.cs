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
    public partial class CheckExisitngFilesPopup : UserControl
    {
        public CheckExisitngFilesPopup()
        {
            InitializeComponent();
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            Utilities.HideCheckExistingFiles();
        }

        private void CheckFiles_Click(object sender, RoutedEventArgs e)
        {
            Utilities.HideCheckExistingFiles();
            Task.Run(() => GameRepair.Start());
        }
    }
}