using launcher.Core.Models;
using launcher.Services;
using System.Windows;
using System.Windows.Controls;
using static launcher.Core.AppControllerService;

namespace launcher
{
    public partial class Popup_Launcher_Update : UserControl
    {
        public Popup_Launcher_Update()
        {
            InitializeComponent();
        }

        public void SetUpdateText(string text, RemoteConfig RemoteConfig)
        {
            Msg.Text = text;

            if (RemoteConfig != null)
            {
                UpdateLater.Visibility = RemoteConfig.forceUpdates ? Visibility.Hidden : Visibility.Visible;
                closeX.Visibility = RemoteConfig.forceUpdates ? Visibility.Hidden : Visibility.Visible;
            }
        }

        private void UpdateLauncher_Click(object sender, RoutedEventArgs e)
        {
            UpdateService.wantsToUpdate = true;
            UpdateService.launcherPopupOpened = false;
        }

        private void UpdateLater_Click(object sender, RoutedEventArgs e)
        {
            UpdateService.wantsToUpdate = false;
            UpdateService.launcherPopupOpened = false;
            HideLauncherUpdatePopup();
        }
    }
}