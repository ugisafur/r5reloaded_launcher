using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using static launcher.Services.LoggerService;
using static launcher.Core.UiReferences;
using static launcher.Core.AppControllerService;
using launcher.Services;
using launcher.GameManagement;
using launcher.Views.Popups.Models.EULA;

namespace launcher
{
    /// <summary>
    /// Interaction logic for EULAPopup.xaml
    /// </summary>
    public partial class Popup_EULA : UserControl
    {
        public Popup_EULA()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public void SetupEULA()
        {
            if (!Launcher.IsOnline)
            {
                LogError( LogSource.Launcher, "Failed to get EULA, no internet connection");
                appDispatcher.BeginInvoke(new Action(() => EULATextBox.Text = "Failed to get EULA, no internet connection"));
                return;
            }

            if (!NetworkHealthService.IsMasterServerAvailableAsync().Result)
            {
                LogError( LogSource.Launcher, "Failed to get EULA, no reponse from master server");
                appDispatcher.BeginInvoke(new Action(() => EULATextBox.Text = "Failed to get EULA, no reponse from master server"));
                return;
            }

            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            HttpResponseMessage response = NetworkHealthService.HttpClient.PostAsync("https://r5r.org/eula", content).Result;

            if (response.IsSuccessStatusCode)
            {
                LogInfo( LogSource.Launcher, "Successfully got EULA");
                EULA euladata = JsonConvert.DeserializeObject<EULA>(response.Content.ReadAsStringAsync().Result);
                appDispatcher.BeginInvoke(new Action(() => EULATextBox.Text = euladata.data.contents));
            }
            else
            {
                LogError( LogSource.Launcher, "Failed to get EULA");
            }
        }

        private void acknowledge_Click(object sender, RoutedEventArgs e)
        {
            ReleaseChannelService.SetEULAAccepted(true);
            Task.Run(() => GameInstaller.Start());
            HideEULA();
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            HideEULA();
        }
    }
}