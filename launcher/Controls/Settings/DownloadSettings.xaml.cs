using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace launcher
{
    /// <summary>
    /// Interaction logic for DownloadSettings.xaml
    /// </summary>
    public partial class DownloadSettings : UserControl
    {
        private int[] downloadSpeeds =
        {
            1000,
            100,
            50,
            20,
            15,
            10,
            5
        };

        public DownloadSettings()
        {
            InitializeComponent();
        }

        public void SetupDownloadSettings()
        {
            // Set the initial state of the toggle switches
            MaxSpeed.Text = Utilities.GetIniSetting(Utilities.IniSettings.Download_Speed_Limit, "");
            ConDownloads.SelectedIndex = Array.IndexOf(downloadSpeeds, Utilities.GetIniSetting(Utilities.IniSettings.Concurrent_Downloads, 1000));

            MaxSpeed.TextChanged += MaxSpeed_TextChanged;
            MaxSpeed.PreviewTextInput += NumericTextBox_PreviewTextInput;
            MaxSpeed.PreviewKeyDown += NumericTextBox_PreviewKeyDown;

            ConDownloads.SelectionChanged += ConDownloads_SelectionChanged;
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            // Regular expression for numeric input with optional leading '-'
            string text = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            e.Handled = !Regex.IsMatch(text, @"^\d*$");
        }

        private void NumericTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true; // Prevent spaces
            }
        }

        private void MaxSpeed_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Utilities.GetIniSetting(Utilities.IniSettings.Download_Speed_Limit, "") != MaxSpeed.Text)
                Utilities.SetIniSetting(Utilities.IniSettings.Download_Speed_Limit, MaxSpeed.Text);
        }

        private void ConDownloads_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Utilities.GetIniSetting(Utilities.IniSettings.Concurrent_Downloads, 1000) != downloadSpeeds[ConDownloads.SelectedIndex])
                Utilities.SetIniSetting(Utilities.IniSettings.Concurrent_Downloads, downloadSpeeds[ConDownloads.SelectedIndex]);
        }
    }
}