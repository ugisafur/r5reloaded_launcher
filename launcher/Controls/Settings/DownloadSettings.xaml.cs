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
        private readonly int[] downloadSpeeds =
        [
            300,
            100,
            50,
            20,
            15,
            10,
            5
        ];

        public DownloadSettings()
        {
            InitializeComponent();
        }

        public void SetupDownloadSettings()
        {
            int conDownloadsLimit = (int)Ini.Get(Ini.Vars.Concurrent_Downloads);
            if (conDownloadsLimit > 300)
                Ini.Set(Ini.Vars.Concurrent_Downloads, 300);

            MaxSpeed.Text = $"{(int)Ini.Get(Ini.Vars.Download_Speed_Limit)}";
            ConDownloads.SelectedIndex = Array.IndexOf(downloadSpeeds, (int)Ini.Get(Ini.Vars.Concurrent_Downloads));

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
            if (((int)Ini.Get(Ini.Vars.Download_Speed_Limit)).ToString() != MaxSpeed.Text)
                Ini.Set(Ini.Vars.Download_Speed_Limit, MaxSpeed.Text);
        }

        private void ConDownloads_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((int)Ini.Get(Ini.Vars.Concurrent_Downloads) != downloadSpeeds[ConDownloads.SelectedIndex])
                Ini.Set(Ini.Vars.Concurrent_Downloads, downloadSpeeds[ConDownloads.SelectedIndex]);
        }
    }
}