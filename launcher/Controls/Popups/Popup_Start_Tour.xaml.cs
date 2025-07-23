using launcher.Configuration;
using System.Windows;
using System.Windows.Controls;
using static launcher.Core.AppController;

namespace launcher
{
    public partial class Popup_Start_Tour : UserControl
    {
        public Popup_Start_Tour()
        {
            InitializeComponent();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            IniSettings.Set(IniSettings.Vars.Ask_For_Tour, false);
            HideOnBoardAskPopup();
            StartTour();
        }

        private void Skip_Click(object sender, RoutedEventArgs e)
        {
            IniSettings.Set(IniSettings.Vars.Ask_For_Tour, false);
            HideOnBoardAskPopup();
            EndTour();
        }
    }
}