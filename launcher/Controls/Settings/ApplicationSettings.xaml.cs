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
            CloseToQuit.IsChecked = (bool)Ini.Get(Ini.Vars.Enable_Quit_On_Close);
            Notifications.IsChecked = (bool)Ini.Get(Ini.Vars.Enable_Notifications);
            KeepAllLogs.IsChecked = (bool)Ini.Get(Ini.Vars.Keep_All_Logs);
            StreamVideo.IsChecked = (bool)Ini.Get(Ini.Vars.Stream_Video);
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

        private void Notifications_Unchecked(object sender, RoutedEventArgs e)
        {
            Ini.Set(Ini.Vars.Enable_Notifications, Notifications.IsChecked.Value);
        }

        private void KeepAllLogs_Unchecked(object sender, RoutedEventArgs e)
        {
            Ini.Set(Ini.Vars.Keep_All_Logs, KeepAllLogs.IsChecked.Value);
        }

        private void StreamVideo_Unchecked(object sender, RoutedEventArgs e)
        {
            Ini.Set(Ini.Vars.Stream_Video, StreamVideo.IsChecked.Value);
        }
    }
}