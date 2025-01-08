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

namespace launcher
{
    /// <summary>
    /// Interaction logic for AccessibilitySettings.xaml
    /// </summary>
    public partial class AccessibilitySettings : UserControl
    {
        public AccessibilitySettings()
        {
            InitializeComponent();
        }

        public void SetupAccessibilitySettings()
        {
            // Set the initial state of the toggle switches
            DisableTransitionsBtn.IsChecked = Utilities.GetIniSetting(Utilities.IniSettings.Disable_Transitions, false);
            DisableAnimationsBtn.IsChecked = Utilities.GetIniSetting(Utilities.IniSettings.Disable_Animations, false);
            DisableBackgroundVideoBtn.IsChecked = Utilities.GetIniSetting(Utilities.IniSettings.Disable_Background_Video, false);
        }

        private void DisableBackgroundVideoBtn_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool value = DisableBackgroundVideoBtn.IsChecked.Value;
            Utilities.SetIniSetting(Utilities.IniSettings.Disable_Background_Video, value);
            Utilities.ToggleBackgroundVideo(DisableBackgroundVideoBtn.IsChecked.Value);
        }

        private void DisableAnimationsBtn_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool value = DisableAnimationsBtn.IsChecked.Value;
            Utilities.SetIniSetting(Utilities.IniSettings.Disable_Animations, value);
        }

        private void DisableTransitionsBtn_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool value = DisableTransitionsBtn.IsChecked.Value;
            Utilities.SetIniSetting(Utilities.IniSettings.Disable_Transitions, value);
        }
    }
}