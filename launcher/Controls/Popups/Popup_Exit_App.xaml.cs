using launcher.BranchUtils;
using System.Windows;
using System.Windows.Controls;
using launcher.Game;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using Hardcodet.Wpf.TaskbarNotification;
using static launcher.Global.References;
using launcher.Managers;
using launcher.Global;

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
            Ini.Set(Ini.Vars.Enable_Quit_On_Close, "quit");
            Application.Current.Shutdown();
        }

        private void Tray_Click(object sender, RoutedEventArgs e)
        {
            Ini.Set(Ini.Vars.Enable_Quit_On_Close, "tray");
            Managers.App.HideAskToQuit();
            Managers.App.SendNotification("Launcher minimized to tray.", BalloonIcon.Info);
            Main_Window.OnClose();
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            Managers.App.HideAskToQuit();
        }
    }
}