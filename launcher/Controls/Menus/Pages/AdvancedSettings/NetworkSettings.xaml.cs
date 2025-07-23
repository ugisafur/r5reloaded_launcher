using launcher.Configuration;
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
            NoTimeout.IsChecked = (bool)IniSettings.Get(IniSettings.Vars.No_Timeout);
            RandomNetKey.IsChecked = (bool)IniSettings.Get(IniSettings.Vars.Random_Netkey);
            PacketsQueued.IsChecked = (bool)IniSettings.Get(IniSettings.Vars.Queued_Packets);
            PacketsEncrypt.IsChecked = (bool)IniSettings.Get(IniSettings.Vars.Encrypt_Packets);

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
            if ((bool)IniSettings.Get(IniSettings.Vars.No_Timeout) != NoTimeout.IsChecked.Value)
                IniSettings.Set(IniSettings.Vars.No_Timeout, NoTimeout.IsChecked.Value);
        }

        private void RandomNetKey_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)IniSettings.Get(IniSettings.Vars.Random_Netkey) != RandomNetKey.IsChecked.Value)
                IniSettings.Set(IniSettings.Vars.Random_Netkey, RandomNetKey.IsChecked.Value);
        }

        private void PacketsQueued_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)IniSettings.Get(IniSettings.Vars.Queued_Packets) != PacketsQueued.IsChecked.Value)
                IniSettings.Set(IniSettings.Vars.Queued_Packets, PacketsQueued.IsChecked.Value);
        }

        private void PacketsEncrypt_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)IniSettings.Get(IniSettings.Vars.Encrypt_Packets) != PacketsEncrypt.IsChecked.Value)
                IniSettings.Set(IniSettings.Vars.Encrypt_Packets, PacketsEncrypt.IsChecked.Value);
        }
    }
}