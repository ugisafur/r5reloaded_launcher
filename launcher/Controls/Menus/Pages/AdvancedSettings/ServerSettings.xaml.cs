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
    /// Interaction logic for ServerSettings.xaml
    /// </summary>
    public partial class ServerSettings : UserControl
    {
        public ServerSettings()
        {
            InitializeComponent();
        }

        public void SetupServerSettings()
        {
            HostName.Text = (string)Ini.Get(Ini.Vars.HostName);
            PlaylistsFile.Text = (string)Ini.Get(Ini.Vars.Playlists_File);
            MapCmb.SelectedIndex = (int)Ini.Get(Ini.Vars.Map);
            PlaylistCmb.SelectedIndex = (int)Ini.Get(Ini.Vars.Playlist);
            VisibilityCmb.SelectedIndex = (int)Ini.Get(Ini.Vars.Visibility);
            Mode.SelectedIndex = (int)Ini.Get(Ini.Vars.Mode);

            HostName.LostKeyboardFocus += HostName_LostFocus;
            PlaylistsFile.LostKeyboardFocus += PlaylistsFile_LostFocus;

            MapCmb.SelectionChanged += Map_SelectionChanged;
            PlaylistCmb.SelectionChanged += Playlist_SelectionChanged;
            VisibilityCmb.SelectionChanged += Visibility_SelectionChanged;
            Mode.SelectionChanged += Mode_SelectionChanged;
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Keyboard.ClearFocus();
                e.Handled = true; // Prevents the beep sound on Enter key
            }
        }

        public void SetMapList(List<string> maps)
        {
            MapCmb.ItemsSource = maps;
            MapCmb.SelectedIndex = 0;
        }

        public void SetPlaylistList(List<string> playlists)
        {
            PlaylistCmb.ItemsSource = playlists;
            PlaylistCmb.SelectedIndex = 0;
        }

        private void HostName_LostFocus(object sender, RoutedEventArgs e)
        {
            if ((string)Ini.Get(Ini.Vars.HostName) != HostName.Text)
                Ini.Set(Ini.Vars.HostName, HostName.Text);
        }

        private void PlaylistsFile_LostFocus(object sender, RoutedEventArgs e)
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
    }
}