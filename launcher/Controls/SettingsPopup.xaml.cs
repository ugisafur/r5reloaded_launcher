using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static launcher.Global;
using static launcher.ControlReferences;

namespace launcher
{
    /// <summary>
    /// Interaction logic for SettingsPopup.xaml
    /// </summary>
    public partial class SettingsPopup : UserControl
    {
        public SettingsPopup()
        {
            InitializeComponent();
        }

        private void btnRepair_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => GameRepair.Start());
        }

        private void AdvancedOptions_Click(object sender, RoutedEventArgs e)
        {
            if (!IN_ADVANCED_MENU)
            {
                gameSettingsPopup.IsOpen = false;
                Utilities.ShowAdvancedControl();
            }
        }
    }
}