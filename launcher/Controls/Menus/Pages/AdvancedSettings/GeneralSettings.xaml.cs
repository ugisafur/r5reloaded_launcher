using launcher.Configuration;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace launcher
{
    /// <summary>
    /// Interaction logic for ApplicationSettings.xaml
    /// </summary>
    public partial class GeneralSettings : UserControl
    {
        public GeneralSettings()
        {
            InitializeComponent();
        }

        public void SetupGeneralSettings()
        {
            ColoredCon.IsChecked = (bool)IniSettings.Get(IniSettings.Vars.Color_Console);
            ConsoleShow.IsChecked = (bool)IniSettings.Get(IniSettings.Vars.Show_Console);
            Dev.IsChecked = (bool)IniSettings.Get(IniSettings.Vars.Enable_Developer);
            Cheats.IsChecked = (bool)IniSettings.Get(IniSettings.Vars.Enable_Cheats);
            Offline.IsChecked = (bool)IniSettings.Get(IniSettings.Vars.Offline_Mode);

            ColoredCon.Unchecked += ColoredCon_Unchecked;
            ConsoleShow.Unchecked += Console_Unchecked;
            Dev.Unchecked += Dev_Unchecked;
            Cheats.Unchecked += Cheats_Unchecked;
            Offline.Unchecked += Offline_Unchecked;

            ColoredCon.Checked += ColoredCon_Unchecked;
            ConsoleShow.Checked += Console_Unchecked;
            Dev.Checked += Dev_Unchecked;
            Cheats.Checked += Cheats_Unchecked;
            Offline.Checked += Offline_Unchecked;
        }

        private void ColoredCon_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)IniSettings.Get(IniSettings.Vars.Color_Console) != ColoredCon.IsChecked.Value)
                IniSettings.Set(IniSettings.Vars.Color_Console, ColoredCon.IsChecked.Value);
        }

        private void Console_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)IniSettings.Get(IniSettings.Vars.Show_Console) != ConsoleShow.IsChecked.Value)
                IniSettings.Set(IniSettings.Vars.Show_Console, ConsoleShow.IsChecked.Value);
        }

        private void Dev_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)IniSettings.Get(IniSettings.Vars.Enable_Developer) != Dev.IsChecked.Value)
                IniSettings.Set(IniSettings.Vars.Enable_Developer, Dev.IsChecked.Value);
        }

        private void Cheats_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)IniSettings.Get(IniSettings.Vars.Enable_Cheats) != Cheats.IsChecked.Value)
                IniSettings.Set(IniSettings.Vars.Enable_Cheats, Cheats.IsChecked.Value);
        }

        private void Offline_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((bool)IniSettings.Get(IniSettings.Vars.Offline_Mode) != Offline.IsChecked.Value)
                IniSettings.Set(IniSettings.Vars.Offline_Mode, Offline.IsChecked.Value);
        }
    }
}