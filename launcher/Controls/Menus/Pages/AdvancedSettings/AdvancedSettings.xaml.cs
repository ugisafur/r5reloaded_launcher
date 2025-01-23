using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using launcher.Classes.Utilities;
using static launcher.Classes.Utilities.Logger;

namespace launcher
{
    /// <summary>
    /// Interaction logic for ApplicationSettings.xaml
    /// </summary>
    public partial class AdvancedSettings : UserControl
    {
        public AdvancedSettings()
        {
            InitializeComponent();
        }

        public void SetupAdvancedSettings()
        {
            CommandLine.Text = (string)Ini.Get(Ini.Vars.Command_Line);

            CommandLine.LostKeyboardFocus += CommandLine_LostFocus;
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Keyboard.ClearFocus();
                e.Handled = true; // Prevents the beep sound on Enter key
            }
        }

        private void CommandLine_LostFocus(object sender, RoutedEventArgs e)
        {
            if ((string)Ini.Get(Ini.Vars.Command_Line) != CommandLine.Text)
                Ini.Set(Ini.Vars.Command_Line, CommandLine.Text);
        }
    }
}