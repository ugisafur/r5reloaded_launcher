using launcher.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            CommandLine.Text = (string)IniSettings.Get(IniSettings.Vars.Command_Line);

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
            if ((string)IniSettings.Get(IniSettings.Vars.Command_Line) != CommandLine.Text)
                IniSettings.Set(IniSettings.Vars.Command_Line, CommandLine.Text);
        }
    }
}