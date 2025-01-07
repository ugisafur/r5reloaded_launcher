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
            SettingsGlobal.DisableBackgroundVideoBtn = DisableBackgroundVideoBtn;
            SettingsGlobal.DisableAnimationsBtn = DisableAnimationsBtn;
            SettingsGlobal.DisableTransitionsBtn = DisableTransitionsBtn;

            // Set the initial state of the toggle switches
            DisableTransitionsBtn.IsChecked = SettingsGlobal.DisableTransitions;
            DisableAnimationsBtn.IsChecked = SettingsGlobal.DisableAnimations;
            DisableBackgroundVideoBtn.IsChecked = SettingsGlobal.DisableBackgroundVideo;
        }

        private void DisableBackgroundVideoBtn_CheckedChanged(object sender, RoutedEventArgs e)
        {
            SettingsGlobal.DisableBackgroundVideo = DisableBackgroundVideoBtn.IsChecked.Value;
            Utilities.ToggleBackgroundVideo(DisableBackgroundVideoBtn.IsChecked.Value);
        }

        private void DisableAnimationsBtn_CheckedChanged(object sender, RoutedEventArgs e)
        {
            //TODO: Implement the DisableAnimationsBtn_CheckedChanged method
            SettingsGlobal.DisableAnimations = DisableAnimationsBtn.IsChecked.Value;
        }

        private void DisableTransitionsBtn_CheckedChanged(object sender, RoutedEventArgs e)
        {
            SettingsGlobal.DisableTransitions = DisableTransitionsBtn.IsChecked.Value;
        }
    }
}