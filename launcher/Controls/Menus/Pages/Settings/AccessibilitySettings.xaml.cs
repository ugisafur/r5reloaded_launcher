using System.Windows;
using System.Windows.Controls;
using static launcher.Classes.Utilities.Logger;
using static launcher.Classes.Global.References;
using launcher.Classes.Utilities;

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
            DisableTransitionsBtn.IsChecked = (bool)Ini.Get(Ini.Vars.Disable_Transitions);
            DisableAnimationsBtn.IsChecked = (bool)Ini.Get(Ini.Vars.Disable_Animations);
            DisableBackgroundVideoBtn.IsChecked = (bool)Ini.Get(Ini.Vars.Disable_Background_Video);

            DisableAnimationsBtn.Checked += DisableAnimationsBtn_CheckedChanged;
            DisableTransitionsBtn.Checked += DisableTransitionsBtn_CheckedChanged;
            DisableBackgroundVideoBtn.Checked += DisableBackgroundVideoBtn_CheckedChanged;

            DisableAnimationsBtn.Unchecked += DisableAnimationsBtn_CheckedChanged;
            DisableTransitionsBtn.Unchecked += DisableTransitionsBtn_CheckedChanged;
            DisableBackgroundVideoBtn.Unchecked += DisableBackgroundVideoBtn_CheckedChanged;
        }

        private void DisableBackgroundVideoBtn_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool value = DisableBackgroundVideoBtn.IsChecked.Value;
            Ini.Set(Ini.Vars.Disable_Background_Video, value);
            ToggleBackgroundVideo(DisableBackgroundVideoBtn.IsChecked.Value);
        }

        private void DisableAnimationsBtn_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool value = DisableAnimationsBtn.IsChecked.Value;
            Ini.Set(Ini.Vars.Disable_Animations, value);
        }

        private void DisableTransitionsBtn_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool value = DisableTransitionsBtn.IsChecked.Value;
            Ini.Set(Ini.Vars.Disable_Transitions, value);
        }

        private static void ToggleBackgroundVideo(bool disabled)
        {
            LogInfo(Source.Launcher, $"Toggling background video: {disabled}");
            Background_Video.Visibility = disabled ? Visibility.Hidden : Visibility.Visible;
            Background_Image.Visibility = disabled ? Visibility.Visible : Visibility.Hidden;
        }
    }
}