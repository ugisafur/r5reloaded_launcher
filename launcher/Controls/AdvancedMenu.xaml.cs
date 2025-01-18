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
            if (AppState.InAdvancedMenu)
                Utilities.HideAdvancedControl();
        }

        public void SetupAdvancedSettings()
        {
            CommandLine.Text = (string)Ini.Get(Ini.Vars.Command_Line);
            HeightRes.Text = (string)Ini.Get(Ini.Vars.Resolution_Height);
            WidthRes.Text = (string)Ini.Get(Ini.Vars.Resolution_Width);
            ThreadsWorker.Text = (string)Ini.Get(Ini.Vars.Worker_Threads);
            AffinityProc.Text = (string)Ini.Get(Ini.Vars.Processor_Affinity);
            ReservedCores.Text = (string)Ini.Get(Ini.Vars.Reserved_Cores);
            MaxFPS.Text = (string)Ini.Get(Ini.Vars.Max_FPS);
            HostName.Text = (string)Ini.Get(Ini.Vars.HostName);
            PlaylistsFile.Text = (string)Ini.Get(Ini.Vars.Playlists_File);
            MapCmb.SelectedIndex = (int)Ini.Get(Ini.Vars.Map);
            PlaylistCmb.SelectedIndex = (int)Ini.Get(Ini.Vars.Playlist);
            VisibilityCmb.SelectedIndex = (int)Ini.Get(Ini.Vars.Visibility);
            Mode.SelectedIndex = (int)Ini.Get(Ini.Vars.Mode);
            Borderless.IsChecked = (bool)Ini.Get(Ini.Vars.Borderless);
            Windowed.IsChecked = (bool)Ini.Get(Ini.Vars.Windowed);
            NoAsync.IsChecked = (bool)Ini.Get(Ini.Vars.No_Async);
            NoTimeout.IsChecked = (bool)Ini.Get(Ini.Vars.No_Timeout);
            RandomNetKey.IsChecked = (bool)Ini.Get(Ini.Vars.Random_Netkey);
            PacketsQueued.IsChecked = (bool)Ini.Get(Ini.Vars.Queued_Packets);
            PacketsEncrypt.IsChecked = (bool)Ini.Get(Ini.Vars.Encrypt_Packets);
            ColoredCon.IsChecked = (bool)Ini.Get(Ini.Vars.Color_Console);
            ConsoleShow.IsChecked = (bool)Ini.Get(Ini.Vars.Show_Console);
            Dev.IsChecked = (bool)Ini.Get(Ini.Vars.Enable_Developer);
            Cheats.IsChecked = (bool)Ini.Get(Ini.Vars.Enable_Cheats);

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

        public void SetMapList(List<string> maps)
        {
            MapCmb.Items.Clear();

            MapCmb.Items.Add("No Selection");

            foreach (string map in maps)
            {
                MapCmb.Items.Add(map);
            }

            MapCmb.SelectedIndex = 0;
        }

        public void SetPlaylistList(List<string> playlists)
        {
            PlaylistCmb.Items.Clear();

            PlaylistCmb.Items.Add("No Selection");

            foreach (string playlist in playlists)
            {
                PlaylistCmb.Items.Add(playlist);
            }

            PlaylistCmb.SelectedIndex = 0;
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
            if ((string)Ini.Get(Ini.Vars.Command_Line) != CommandLine.Text)
                Ini.Set(Ini.Vars.Command_Line, CommandLine.Text);
        }

        private void Height_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (HeightRes.Text == "-")
                return;

            if ((string)Ini.Get(Ini.Vars.Resolution_Height) != HeightRes.Text)
                Ini.Set(Ini.Vars.Resolution_Height, HeightRes.Text);
        }

        private void Width_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (WidthRes.Text == "-")
                return;

            if ((string)Ini.Get(Ini.Vars.Resolution_Width) != WidthRes.Text)
                Ini.Set(Ini.Vars.Resolution_Width, WidthRes.Text);
        }

        private void Threads_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(ThreadsWorker.Text) || ThreadsWorker.Text == "-")
                return;

            if ((string)Ini.Get(Ini.Vars.Worker_Threads) != ThreadsWorker.Text)
                Ini.Set(Ini.Vars.Worker_Threads, ThreadsWorker.Text);
        }

        private void Affinity_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(AffinityProc.Text) || AffinityProc.Text == "-")
                return;

            if ((string)Ini.Get(Ini.Vars.Processor_Affinity) != AffinityProc.Text)
                Ini.Set(Ini.Vars.Processor_Affinity, AffinityProc.Text);
        }

        private void ReservedCores_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(ReservedCores.Text) || ReservedCores.Text == "-")
                return;

            if ((string)Ini.Get(Ini.Vars.Reserved_Cores) != ReservedCores.Text)
                Ini.Set(Ini.Vars.Reserved_Cores, ReservedCores.Text);
        }

        private void MaxFPS_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(MaxFPS.Text) || MaxFPS.Text == "-")
                return;

            if ((string)Ini.Get(Ini.Vars.Max_FPS) != MaxFPS.Text)
                Ini.Set(Ini.Vars.Max_FPS, MaxFPS.Text);
        }

        private void HostName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((string)Ini.Get(Ini.Vars.HostName) != HostName.Text)
                Ini.Set(Ini.Vars.HostName, HostName.Text);
        }

        private void PlaylistsFile_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((string)Ini.Get(Ini.Vars.Playlists_File) != PlaylistsFile.Text)
                Ini.Set(Ini.Vars.Playlists_File, PlaylistsFile.Text);
        }

        private void Map_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((int)Ini.Get(Ini.Vars.Map) != MapCmb.SelectedIndex)
                Ini.Set(Ini.Vars.Map, MapCmb.SelectedIndex);
        }

        private void Playlist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((int)Ini.Get(Ini.Vars.Playlist) != PlaylistCmb.SelectedIndex)
                Ini.Set(Ini.Vars.Playlist, PlaylistCmb.SelectedIndex);
        }

        private void Visibility_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((int)Ini.Get(Ini.Vars.Visibility) != VisibilityCmb.SelectedIndex)
                Ini.Set(Ini.Vars.Visibility, VisibilityCmb.SelectedIndex);
        }

        private void Mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((int)Ini.Get(Ini.Vars.Mode) != Mode.SelectedIndex)
                Ini.Set(Ini.Vars.Mode, Mode.SelectedIndex);
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

        private void NoAsync_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)Ini.Get(Ini.Vars.No_Async) != NoAsync.IsChecked.Value)
                Ini.Set(Ini.Vars.No_Async, NoAsync.IsChecked.Value);
        }

        private void NoTimeout_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)Ini.Get(Ini.Vars.No_Timeout) != NoTimeout.IsChecked.Value)
                Ini.Set(Ini.Vars.No_Timeout, NoTimeout.IsChecked.Value);
        }

        private void RandomNetKey_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)Ini.Get(Ini.Vars.Random_Netkey) != RandomNetKey.IsChecked.Value)
                Ini.Set(Ini.Vars.Random_Netkey, RandomNetKey.IsChecked.Value);
        }

        private void PacketsQueued_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)Ini.Get(Ini.Vars.Queued_Packets) != PacketsQueued.IsChecked.Value)
                Ini.Set(Ini.Vars.Queued_Packets, PacketsQueued.IsChecked.Value);
        }

        private void PacketsEncrypt_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)Ini.Get(Ini.Vars.Encrypt_Packets) != PacketsEncrypt.IsChecked.Value)
                Ini.Set(Ini.Vars.Encrypt_Packets, PacketsEncrypt.IsChecked.Value);
        }

        private void ColoredCon_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)Ini.Get(Ini.Vars.Color_Console) != ColoredCon.IsChecked.Value)
                Ini.Set(Ini.Vars.Color_Console, ColoredCon.IsChecked.Value);
        }

        private void Console_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)Ini.Get(Ini.Vars.Show_Console) != ConsoleShow.IsChecked.Value)
                Ini.Set(Ini.Vars.Show_Console, ConsoleShow.IsChecked.Value);
        }

        private void Dev_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)Ini.Get(Ini.Vars.Enable_Developer) != Dev.IsChecked.Value)
                Ini.Set(Ini.Vars.Enable_Developer, Dev.IsChecked.Value);
        }

        private void Cheats_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)Ini.Get(Ini.Vars.Enable_Cheats) != Cheats.IsChecked.Value)
                Ini.Set(Ini.Vars.Enable_Cheats, Cheats.IsChecked.Value);
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}