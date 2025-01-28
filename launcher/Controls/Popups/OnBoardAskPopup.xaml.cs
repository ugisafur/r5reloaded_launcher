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
    public partial class OnBoardAskPopup : UserControl
    {
        public OnBoardAskPopup()
        {
            InitializeComponent();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            Ini.Set(Ini.Vars.Ask_For_Tour, false);
            Managers.App.HideOnBoardAskPopup();
            Managers.App.StartTour();
        }

        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            Ini.Set(Ini.Vars.Ask_For_Tour, false);
            Managers.App.HideOnBoardAskPopup();
            Managers.App.EndTour();
        }
    }
}