using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using static launcher.Logger;

namespace launcher
{
    /// <summary>
    /// Interaction logic for ApplicationSettings.xaml
    /// </summary>
    public partial class ApplicationSettings : UserControl
    {
        public ApplicationSettings()
        {
            InitializeComponent();
        }

        public void SetupApplicationSettings()
        {
            // Set the initial state of the toggle switches
            CloseToQuit.IsChecked = Ini.Get(Ini.Vars.Enable_Quit_On_Close, false);
        }

        private void GetLogs_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", @System.IO.Path.GetDirectoryName(LogFilePath));
        }

        private void ClearCache_Click(object sender, RoutedEventArgs e)
        {
            //TODO
        }

        private void CloseToQuit_Unchecked(object sender, RoutedEventArgs e)
        {
            Ini.Set(Ini.Vars.Enable_Quit_On_Close, CloseToQuit.IsChecked.Value);
        }
    }
}