using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using static launcher.Core.UiReferences;
using static launcher.Core.AppControllerService;
using launcher.Services;

namespace launcher
{
    public partial class Popup_Exit_App : UserControl
    {
        public Popup_Exit_App()
        {
            InitializeComponent();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            SettingsService.Set(SettingsService.Vars.Enable_Quit_On_Close, "quit");
            Application.Current.Shutdown();
        }

        private void Tray_Click(object sender, RoutedEventArgs e)
        {
            SettingsService.Set(SettingsService.Vars.Enable_Quit_On_Close, "tray");
            HideAskToQuit();
            SendNotification("Launcher minimized to tray.", BalloonIcon.Info);
            Main_Window.OnClose();
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            HideAskToQuit();
        }
    }
}