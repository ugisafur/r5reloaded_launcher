using launcher.Services;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace launcher
{
    /// <summary>
    /// Interaction logic for ApplicationSettings.xaml
    /// </summary>
    public partial class GraphicsSettings : UserControl
    {
        public GraphicsSettings()
        {
            InitializeComponent();
        }

        public void SetupGraphicsSettings()
        {
            HeightRes.Text = (string)SettingsService.Get(SettingsService.Vars.Resolution_Height);
            WidthRes.Text = (string)SettingsService.Get(SettingsService.Vars.Resolution_Width);
            MaxFPS.Text = (string)SettingsService.Get(SettingsService.Vars.Max_FPS);
            Borderless.IsChecked = (bool)SettingsService.Get(SettingsService.Vars.Borderless);
            Windowed.IsChecked = (bool)SettingsService.Get(SettingsService.Vars.Windowed);

            HeightRes.LostKeyboardFocus += Height_LostFocus;
            WidthRes.LostKeyboardFocus += Width_LostFocus;
            MaxFPS.LostKeyboardFocus += MaxFPS_LostFocus;

            Borderless.Checked += Borderless_Unchecked;
            Windowed.Checked += Windowed_Unchecked;

            Borderless.Unchecked += Borderless_Unchecked;
            Windowed.Unchecked += Windowed_Unchecked;
        }

        private void NumericTextBox2_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            // Regular expression for numeric input
            string text = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            e.Handled = !Regex.IsMatch(text, @"^\d*$");
        }

        private void NumericTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true; // Prevent spaces
            }

            if (e.Key == Key.Enter)
            {
                Keyboard.ClearFocus();
                e.Handled = true; // Prevents the beep sound on Enter key
            }
        }

        private void Height_LostFocus(object sender, RoutedEventArgs e)
        {
            if ((string)SettingsService.Get(SettingsService.Vars.Resolution_Height) != HeightRes.Text)
                SettingsService.Set(SettingsService.Vars.Resolution_Height, HeightRes.Text);
        }

        private void Width_LostFocus(object sender, RoutedEventArgs e)
        {
            if ((string)SettingsService.Get(SettingsService.Vars.Resolution_Width) != WidthRes.Text)
                SettingsService.Set(SettingsService.Vars.Resolution_Width, WidthRes.Text);
        }

        private void MaxFPS_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(MaxFPS.Text))
                MaxFPS.Text = "0";

            if ((string)SettingsService.Get(SettingsService.Vars.Max_FPS) != MaxFPS.Text)
                SettingsService.Set(SettingsService.Vars.Max_FPS, MaxFPS.Text);
        }

        private void Borderless_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)SettingsService.Get(SettingsService.Vars.Borderless) != Borderless.IsChecked.Value)
                SettingsService.Set(SettingsService.Vars.Borderless, Borderless.IsChecked.Value);
        }

        private void Windowed_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)SettingsService.Get(SettingsService.Vars.Windowed) != Windowed.IsChecked.Value)
                SettingsService.Set(SettingsService.Vars.Windowed, Windowed.IsChecked.Value);
        }
    }
}