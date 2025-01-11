using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Input;
using static launcher.Global;

namespace launcher
{
    /// <summary>
    /// Interaction logic for AdvancedMenu.xaml
    /// </summary>
    public partial class AdvancedMenu : UserControl
    {
        public AdvancedMenu()
        {
            InitializeComponent();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (IN_ADVANCED_MENU)
                Utilities.HideAdvancedControl();
        }

        public void SetupAdvancedSettings()
        {
            CommandLine.Text = Utilities.GetIniSetting(Utilities.IniSettings.Command_Line, "");
            HeightRes.Text = Utilities.GetIniSetting(Utilities.IniSettings.Resolution_Height, "");
            WidthRes.Text = Utilities.GetIniSetting(Utilities.IniSettings.Resolution_Width, "");
            ThreadsWorker.Text = Utilities.GetIniSetting(Utilities.IniSettings.Worker_Threads, "-1");
            AffinityProc.Text = Utilities.GetIniSetting(Utilities.IniSettings.Processor_Affinity, "-1");
            ReservedCores.Text = Utilities.GetIniSetting(Utilities.IniSettings.Reserved_Cores, "0");
            MaxFPS.Text = Utilities.GetIniSetting(Utilities.IniSettings.Max_FPS, "-1");
            HostName.Text = Utilities.GetIniSetting(Utilities.IniSettings.HostName, "");
            PlaylistsFile.Text = Utilities.GetIniSetting(Utilities.IniSettings.Playlists_File, "playlists_r5_patch.txt");
            MapCmb.SelectedIndex = Utilities.GetIniSetting(Utilities.IniSettings.Map, 0);
            PlaylistCmb.SelectedIndex = Utilities.GetIniSetting(Utilities.IniSettings.Playlist, 0);
            VisibilityCmb.SelectedIndex = Utilities.GetIniSetting(Utilities.IniSettings.Visibility, 0);
            Mode.SelectedIndex = Utilities.GetIniSetting(Utilities.IniSettings.Mode, 0);
            Borderless.IsChecked = Utilities.GetIniSetting(Utilities.IniSettings.Borderless, false);
            Windowed.IsChecked = Utilities.GetIniSetting(Utilities.IniSettings.Windowed, false);
            NoAsync.IsChecked = Utilities.GetIniSetting(Utilities.IniSettings.No_Async, false);
            NoTimeout.IsChecked = Utilities.GetIniSetting(Utilities.IniSettings.No_Timeout, false);
            RandomNetKey.IsChecked = Utilities.GetIniSetting(Utilities.IniSettings.Random_Netkey, false);
            PacketsQueued.IsChecked = Utilities.GetIniSetting(Utilities.IniSettings.Queued_Packets, false);
            PacketsEncrypt.IsChecked = Utilities.GetIniSetting(Utilities.IniSettings.Encrypt_Packets, false);
            ColoredCon.IsChecked = Utilities.GetIniSetting(Utilities.IniSettings.Color_Console, false);
            ConsoleShow.IsChecked = Utilities.GetIniSetting(Utilities.IniSettings.Show_Console, false);
            Dev.IsChecked = Utilities.GetIniSetting(Utilities.IniSettings.Enable_Developer, false);
            Cheats.IsChecked = Utilities.GetIniSetting(Utilities.IniSettings.Enable_Cheats, false);

            CommandLine.TextChanged += CommandLine_TextChanged;
            HeightRes.TextChanged += Height_TextChanged;
            WidthRes.TextChanged += Width_TextChanged;
            ThreadsWorker.TextChanged += Threads_TextChanged;
            AffinityProc.TextChanged += Affinity_TextChanged;
            ReservedCores.TextChanged += ReservedCores_TextChanged;
            MaxFPS.TextChanged += MaxFPS_TextChanged;
            HostName.TextChanged += HostName_TextChanged;
            PlaylistsFile.TextChanged += PlaylistsFile_TextChanged;

            MapCmb.SelectionChanged += Map_SelectionChanged;
            PlaylistCmb.SelectionChanged += Playlist_SelectionChanged;
            VisibilityCmb.SelectionChanged += Visibility_SelectionChanged;
            Mode.SelectionChanged += Mode_SelectionChanged;

            Borderless.Unchecked += Borderless_Unchecked;
            Windowed.Unchecked += Windowed_Unchecked;
            NoAsync.Unchecked += NoAsync_Unchecked;
            NoTimeout.Unchecked += NoTimeout_Unchecked;
            RandomNetKey.Unchecked += RandomNetKey_Unchecked;
            PacketsQueued.Unchecked += PacketsQueued_Unchecked;
            PacketsEncrypt.Unchecked += PacketsEncrypt_Unchecked;
            ColoredCon.Unchecked += ColoredCon_Unchecked;
            ConsoleShow.Unchecked += Console_Unchecked;
            Dev.Unchecked += Dev_Unchecked;
            Cheats.Unchecked += Cheats_Unchecked;

            Borderless.Checked += Borderless_Unchecked;
            Windowed.Checked += Windowed_Unchecked;
            NoAsync.Checked += NoAsync_Unchecked;
            NoTimeout.Checked += NoTimeout_Unchecked;
            RandomNetKey.Checked += RandomNetKey_Unchecked;
            PacketsQueued.Checked += PacketsQueued_Unchecked;
            PacketsEncrypt.Checked += PacketsEncrypt_Unchecked;
            ColoredCon.Checked += ColoredCon_Unchecked;
            ConsoleShow.Checked += Console_Unchecked;
            Dev.Checked += Dev_Unchecked;
            Cheats.Checked += Cheats_Unchecked;
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            // Regular expression for numeric input with optional leading '-'
            string text = textBox.Text.Insert(textBox.SelectionStart, e.Text);
            e.Handled = !Regex.IsMatch(text, @"^-?\d*$");
        }

        private void NumericTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true; // Prevent spaces
            }
        }

        private void CommandLine_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Utilities.GetIniSetting(Utilities.IniSettings.Command_Line, "") != CommandLine.Text)
                Utilities.SetIniSetting(Utilities.IniSettings.Command_Line, CommandLine.Text);
        }

        private void Height_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (HeightRes.Text == "-")
                return;

            if (Utilities.GetIniSetting(Utilities.IniSettings.Resolution_Height, "") != HeightRes.Text)
                Utilities.SetIniSetting(Utilities.IniSettings.Resolution_Height, HeightRes.Text);
        }

        private void Width_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (WidthRes.Text == "-")
                return;

            if (Utilities.GetIniSetting(Utilities.IniSettings.Resolution_Width, "") != WidthRes.Text)
                Utilities.SetIniSetting(Utilities.IniSettings.Resolution_Width, WidthRes.Text);
        }

        private void Threads_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(ThreadsWorker.Text) || ThreadsWorker.Text == "-")
                return;

            if (Utilities.GetIniSetting(Utilities.IniSettings.Worker_Threads, "-1") != ThreadsWorker.Text)
                Utilities.SetIniSetting(Utilities.IniSettings.Worker_Threads, ThreadsWorker.Text);
        }

        private void Affinity_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(AffinityProc.Text) || AffinityProc.Text == "-")
                return;

            if (Utilities.GetIniSetting(Utilities.IniSettings.Processor_Affinity, "-1") != AffinityProc.Text)
                Utilities.SetIniSetting(Utilities.IniSettings.Processor_Affinity, AffinityProc.Text);
        }

        private void ReservedCores_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(ReservedCores.Text) || ReservedCores.Text == "-")
                return;

            if (Utilities.GetIniSetting(Utilities.IniSettings.Reserved_Cores, "0") != ReservedCores.Text)
                Utilities.SetIniSetting(Utilities.IniSettings.Reserved_Cores, ReservedCores.Text);
        }

        private void MaxFPS_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(MaxFPS.Text) || MaxFPS.Text == "-")
                return;

            if (Utilities.GetIniSetting(Utilities.IniSettings.Max_FPS, "-1") != MaxFPS.Text)
                Utilities.SetIniSetting(Utilities.IniSettings.Max_FPS, MaxFPS.Text);
        }

        private void HostName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Utilities.GetIniSetting(Utilities.IniSettings.HostName, "") != HostName.Text)
                Utilities.SetIniSetting(Utilities.IniSettings.HostName, HostName.Text);
        }

        private void PlaylistsFile_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Utilities.GetIniSetting(Utilities.IniSettings.Playlists_File, "playlists_r5_patch.txt") != PlaylistsFile.Text)
                Utilities.SetIniSetting(Utilities.IniSettings.Playlists_File, PlaylistsFile.Text);
        }

        private void Map_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (Utilities.GetIniSetting(Utilities.IniSettings.Map, 0) != MapCmb.SelectedIndex)
            //Utilities.SetIniSetting(Utilities.IniSettings.Map, MapCmb.SelectedIndex);
        }

        private void Playlist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (Utilities.GetIniSetting(Utilities.IniSettings.Playlist, 0) != PlaylistCmb.SelectedIndex)
            //Utilities.SetIniSetting(Utilities.IniSettings.Playlist, PlaylistCmb.SelectedIndex);
        }

        private void Visibility_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Utilities.GetIniSetting(Utilities.IniSettings.Visibility, 0) != VisibilityCmb.SelectedIndex)
                Utilities.SetIniSetting(Utilities.IniSettings.Visibility, VisibilityCmb.SelectedIndex);
        }

        private void Mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Utilities.GetIniSetting(Utilities.IniSettings.Mode, 0) != Mode.SelectedIndex)
                Utilities.SetIniSetting(Utilities.IniSettings.Mode, Mode.SelectedIndex);
        }

        private void Borderless_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Utilities.GetIniSetting(Utilities.IniSettings.Borderless, false) != Borderless.IsChecked.Value)
                Utilities.SetIniSetting(Utilities.IniSettings.Borderless, Borderless.IsChecked.Value);
        }

        private void Windowed_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Utilities.GetIniSetting(Utilities.IniSettings.Windowed, false) != Windowed.IsChecked.Value)
                Utilities.SetIniSetting(Utilities.IniSettings.Windowed, Windowed.IsChecked.Value);
        }

        private void NoAsync_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Utilities.GetIniSetting(Utilities.IniSettings.No_Async, false) != NoAsync.IsChecked.Value)
                Utilities.SetIniSetting(Utilities.IniSettings.No_Async, NoAsync.IsChecked.Value);
        }

        private void NoTimeout_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Utilities.GetIniSetting(Utilities.IniSettings.No_Timeout, false) != NoTimeout.IsChecked.Value)
                Utilities.SetIniSetting(Utilities.IniSettings.No_Timeout, NoTimeout.IsChecked.Value);
        }

        private void RandomNetKey_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Utilities.GetIniSetting(Utilities.IniSettings.Random_Netkey, true) != RandomNetKey.IsChecked.Value)
                Utilities.SetIniSetting(Utilities.IniSettings.Random_Netkey, RandomNetKey.IsChecked.Value);
        }

        private void PacketsQueued_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Utilities.GetIniSetting(Utilities.IniSettings.Queued_Packets, true) != PacketsQueued.IsChecked.Value)
                Utilities.SetIniSetting(Utilities.IniSettings.Queued_Packets, PacketsQueued.IsChecked.Value);
        }

        private void PacketsEncrypt_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Utilities.GetIniSetting(Utilities.IniSettings.Encrypt_Packets, true) != PacketsEncrypt.IsChecked.Value)
                Utilities.SetIniSetting(Utilities.IniSettings.Encrypt_Packets, PacketsEncrypt.IsChecked.Value);
        }

        private void ColoredCon_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Utilities.GetIniSetting(Utilities.IniSettings.Color_Console, false) != ColoredCon.IsChecked.Value)
                Utilities.SetIniSetting(Utilities.IniSettings.Color_Console, ColoredCon.IsChecked.Value);
        }

        private void Console_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Utilities.GetIniSetting(Utilities.IniSettings.Show_Console, false) != ConsoleShow.IsChecked.Value)
                Utilities.SetIniSetting(Utilities.IniSettings.Show_Console, ConsoleShow.IsChecked.Value);
        }

        private void Dev_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Utilities.GetIniSetting(Utilities.IniSettings.Enable_Developer, false) != Dev.IsChecked.Value)
                Utilities.SetIniSetting(Utilities.IniSettings.Enable_Developer, Dev.IsChecked.Value);
        }

        private void Cheats_Unchecked(object sender, RoutedEventArgs e)
        {
            if (Utilities.GetIniSetting(Utilities.IniSettings.Enable_Cheats, false) != Cheats.IsChecked.Value)
                Utilities.SetIniSetting(Utilities.IniSettings.Enable_Cheats, Cheats.IsChecked.Value);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}