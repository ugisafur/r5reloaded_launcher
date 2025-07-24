using launcher.Services;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace launcher
{
    /// <summary>
    /// Interaction logic for ApplicationSettings.xaml
    /// </summary>
    public partial class NetworkSettings : UserControl
    {
        public NetworkSettings()
        {
            InitializeComponent();
        }

        public void SetupNetworkSettings()
        {
            NoTimeout.IsChecked = (bool)SettingsService.Get(SettingsService.Vars.No_Timeout);
            RandomNetKey.IsChecked = (bool)SettingsService.Get(SettingsService.Vars.Random_Netkey);
            PacketsQueued.IsChecked = (bool)SettingsService.Get(SettingsService.Vars.Queued_Packets);
            PacketsEncrypt.IsChecked = (bool)SettingsService.Get(SettingsService.Vars.Encrypt_Packets);

            NoTimeout.Unchecked += NoTimeout_Unchecked;
            RandomNetKey.Unchecked += RandomNetKey_Unchecked;
            PacketsQueued.Unchecked += PacketsQueued_Unchecked;
            PacketsEncrypt.Unchecked += PacketsEncrypt_Unchecked;

            NoTimeout.Checked += NoTimeout_Unchecked;
            RandomNetKey.Checked += RandomNetKey_Unchecked;
            PacketsQueued.Checked += PacketsQueued_Unchecked;
            PacketsEncrypt.Checked += PacketsEncrypt_Unchecked;
        }

        private void NoTimeout_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)SettingsService.Get(SettingsService.Vars.No_Timeout) != NoTimeout.IsChecked.Value)
                SettingsService.Set(SettingsService.Vars.No_Timeout, NoTimeout.IsChecked.Value);
        }

        private void RandomNetKey_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)SettingsService.Get(SettingsService.Vars.Random_Netkey) != RandomNetKey.IsChecked.Value)
                SettingsService.Set(SettingsService.Vars.Random_Netkey, RandomNetKey.IsChecked.Value);
        }

        private void PacketsQueued_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)SettingsService.Get(SettingsService.Vars.Queued_Packets) != PacketsQueued.IsChecked.Value)
                SettingsService.Set(SettingsService.Vars.Queued_Packets, PacketsQueued.IsChecked.Value);
        }

        private void PacketsEncrypt_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)SettingsService.Get(SettingsService.Vars.Encrypt_Packets) != PacketsEncrypt.IsChecked.Value)
                SettingsService.Set(SettingsService.Vars.Encrypt_Packets, PacketsEncrypt.IsChecked.Value);
        }
    }
}