using launcher.Classes.BranchUtils;
using System.Windows;
using System.Windows.Controls;
using launcher.Classes.Global;
using launcher.Classes.Game;
using launcher.Classes.Utilities;
using launcher.Classes.Managers;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using Hardcodet.Wpf.TaskbarNotification;
using static launcher.Classes.Global.References;

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
            AppManager.HideOnBoardAskPopup();
            AppManager.StartTour();
        }

        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            Ini.Set(Ini.Vars.Ask_For_Tour, false);
            AppManager.HideOnBoardAskPopup();
            AppManager.EndTour();
        }
    }
}