using System.Windows;
using System.Windows.Controls;
using static launcher.Services.LoggerService;
using static launcher.Core.UiReferences;
using launcher.Services;

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
            DisableTransitionsBtn.IsChecked = (bool)SettingsService.Get(SettingsService.Vars.Disable_Transitions);
            DisableAnimationsBtn.IsChecked = (bool)SettingsService.Get(SettingsService.Vars.Disable_Animations);
            DisableBackgroundVideoBtn.IsChecked = (bool)SettingsService.Get(SettingsService.Vars.Disable_Background_Video);

            DisableAnimationsBtn.Checked += DisableAnimationsBtn_CheckedChanged;
            DisableTransitionsBtn.Checked += DisableTransitionsBtn_CheckedChanged;
            DisableBackgroundVideoBtn.Checked += DisableBackgroundVideoBtn_CheckedChanged;

            DisableAnimationsBtn.Unchecked += DisableAnimationsBtn_CheckedChanged;
            DisableTransitionsBtn.Unchecked += DisableTransitionsBtn_CheckedChanged;
            DisableBackgroundVideoBtn.Unchecked += DisableBackgroundVideoBtn_CheckedChanged;

            if (appState.wineEnv)
            {
                DisableBackgroundVideoBtn.IsChecked = true;
                DisableBackgroundVideoBtn.IsEnabled = false;
                DisableBackgroundText.Text = "Disable background video - Permanently disabled when running under wine";
            }
        }

        private void DisableBackgroundVideoBtn_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool value = DisableBackgroundVideoBtn.IsChecked.Value;
            SettingsService.Set(SettingsService.Vars.Disable_Background_Video, value);
            ToggleBackgroundVideo(DisableBackgroundVideoBtn.IsChecked.Value);
        }

        private void DisableAnimationsBtn_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool value = DisableAnimationsBtn.IsChecked.Value;
            SettingsService.Set(SettingsService.Vars.Disable_Animations, value);
        }

        private void DisableTransitionsBtn_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool value = DisableTransitionsBtn.IsChecked.Value;
            SettingsService.Set(SettingsService.Vars.Disable_Transitions, value);
        }

        private static void ToggleBackgroundVideo(bool disabled)
        {
            if (appState.wineEnv)
                return;

            LogInfo(LogSource.Launcher, $"Toggling background video: {disabled}");
            Background_Video.Visibility = disabled ? Visibility.Hidden : Visibility.Visible;
        }
    }
}