using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using launcher.Utilities;
using static launcher.Utilities.Logger;

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
            HeightRes.Text = (string)Ini.Get(Ini.Vars.Resolution_Height);
            WidthRes.Text = (string)Ini.Get(Ini.Vars.Resolution_Width);
            MaxFPS.Text = (string)Ini.Get(Ini.Vars.Max_FPS);
            Borderless.IsChecked = (bool)Ini.Get(Ini.Vars.Borderless);
            Windowed.IsChecked = (bool)Ini.Get(Ini.Vars.Windowed);

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
            if ((string)Ini.Get(Ini.Vars.Resolution_Height) != HeightRes.Text)
                Ini.Set(Ini.Vars.Resolution_Height, HeightRes.Text);
        }

        private void Width_LostFocus(object sender, RoutedEventArgs e)
        {
            if ((string)Ini.Get(Ini.Vars.Resolution_Width) != WidthRes.Text)
                Ini.Set(Ini.Vars.Resolution_Width, WidthRes.Text);
        }

        private void MaxFPS_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(MaxFPS.Text))
                MaxFPS.Text = "0";

            if ((string)Ini.Get(Ini.Vars.Max_FPS) != MaxFPS.Text)
                Ini.Set(Ini.Vars.Max_FPS, MaxFPS.Text);
        }

        private void Borderless_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)Ini.Get(Ini.Vars.Borderless) != Borderless.IsChecked.Value)
                Ini.Set(Ini.Vars.Borderless, Borderless.IsChecked.Value);
        }

        private void Windowed_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)Ini.Get(Ini.Vars.Windowed) != Windowed.IsChecked.Value)
                Ini.Set(Ini.Vars.Windowed, Windowed.IsChecked.Value);
        }
    }
}