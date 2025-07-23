using launcher.Configuration;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            HostName.Text = (string)IniSettings.Get(IniSettings.Vars.HostName);
            PlaylistsFile.Text = (string)IniSettings.Get(IniSettings.Vars.Playlists_File);
            MapCmb.SelectedIndex = (int)IniSettings.Get(IniSettings.Vars.Map);
            PlaylistCmb.SelectedIndex = (int)IniSettings.Get(IniSettings.Vars.Playlist);
            VisibilityCmb.SelectedIndex = (int)IniSettings.Get(IniSettings.Vars.Visibility);
            Mode.SelectedIndex = (int)IniSettings.Get(IniSettings.Vars.Mode);

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
            if ((string)IniSettings.Get(IniSettings.Vars.HostName) != HostName.Text)
                IniSettings.Set(IniSettings.Vars.HostName, HostName.Text);
        }

        private void PlaylistsFile_LostFocus(object sender, RoutedEventArgs e)
        {
            if ((string)IniSettings.Get(IniSettings.Vars.Playlists_File) != PlaylistsFile.Text)
                IniSettings.Set(IniSettings.Vars.Playlists_File, PlaylistsFile.Text);
        }

        private void Map_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((int)IniSettings.Get(IniSettings.Vars.Map) != MapCmb.SelectedIndex)
                IniSettings.Set(IniSettings.Vars.Map, MapCmb.SelectedIndex);
        }

        private void Playlist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((int)IniSettings.Get(IniSettings.Vars.Playlist) != PlaylistCmb.SelectedIndex)
                IniSettings.Set(IniSettings.Vars.Playlist, PlaylistCmb.SelectedIndex);
        }

        private void Visibility_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((int)IniSettings.Get(IniSettings.Vars.Visibility) != VisibilityCmb.SelectedIndex)
                IniSettings.Set(IniSettings.Vars.Visibility, VisibilityCmb.SelectedIndex);
        }

        private void Mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((int)IniSettings.Get(IniSettings.Vars.Mode) != Mode.SelectedIndex)
                IniSettings.Set(IniSettings.Vars.Mode, Mode.SelectedIndex);
        }
    }
}