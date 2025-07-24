using launcher.Services;
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
            HostName.Text = (string)SettingsService.Get(SettingsService.Vars.HostName);
            PlaylistsFile.Text = (string)SettingsService.Get(SettingsService.Vars.Playlists_File);
            MapCmb.SelectedIndex = (int)SettingsService.Get(SettingsService.Vars.Map);
            PlaylistCmb.SelectedIndex = (int)SettingsService.Get(SettingsService.Vars.Playlist);
            VisibilityCmb.SelectedIndex = (int)SettingsService.Get(SettingsService.Vars.Visibility);
            Mode.SelectedIndex = (int)SettingsService.Get(SettingsService.Vars.Mode);

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
            if ((string)SettingsService.Get(SettingsService.Vars.HostName) != HostName.Text)
                SettingsService.Set(SettingsService.Vars.HostName, HostName.Text);
        }

        private void PlaylistsFile_LostFocus(object sender, RoutedEventArgs e)
        {
            if ((string)SettingsService.Get(SettingsService.Vars.Playlists_File) != PlaylistsFile.Text)
                SettingsService.Set(SettingsService.Vars.Playlists_File, PlaylistsFile.Text);
        }

        private void Map_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((int)SettingsService.Get(SettingsService.Vars.Map) != MapCmb.SelectedIndex)
                SettingsService.Set(SettingsService.Vars.Map, MapCmb.SelectedIndex);
        }

        private void Playlist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((int)SettingsService.Get(SettingsService.Vars.Playlist) != PlaylistCmb.SelectedIndex)
                SettingsService.Set(SettingsService.Vars.Playlist, PlaylistCmb.SelectedIndex);
        }

        private void Visibility_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((int)SettingsService.Get(SettingsService.Vars.Visibility) != VisibilityCmb.SelectedIndex)
                SettingsService.Set(SettingsService.Vars.Visibility, VisibilityCmb.SelectedIndex);
        }

        private void Mode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((int)SettingsService.Get(SettingsService.Vars.Mode) != Mode.SelectedIndex)
                SettingsService.Set(SettingsService.Vars.Mode, Mode.SelectedIndex);
        }
    }
}