using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using launcher.Classes.Utilities;
using static launcher.Classes.Utilities.Logger;

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
            NoTimeout.IsChecked = (bool)Ini.Get(Ini.Vars.No_Timeout);
            RandomNetKey.IsChecked = (bool)Ini.Get(Ini.Vars.Random_Netkey);
            PacketsQueued.IsChecked = (bool)Ini.Get(Ini.Vars.Queued_Packets);
            PacketsEncrypt.IsChecked = (bool)Ini.Get(Ini.Vars.Encrypt_Packets);

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
    }
}