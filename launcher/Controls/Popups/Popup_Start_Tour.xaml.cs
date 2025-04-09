using System.Windows;
using System.Windows.Controls;
using launcher.Global;

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