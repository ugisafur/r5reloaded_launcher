using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using launcher.Game;
using launcher.Global;
using launcher.Managers;
using launcher.BranchUtils;
using launcher.Network;

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
            if (!AppState.IsOnline)
            {
                Logger.LogError(Logger.Source.Launcher, "Failed to get EULA, no internet connection");
                EULATextBox.Text = "Failed to get EULA, no internet connection";
                return;
            }

            if (!Connection.MasterServerTest())
            {
                Logger.LogError(Logger.Source.Launcher, "Failed to get EULA, no reponse from master server");
                EULATextBox.Text = "Failed to get EULA, no reponse from master server";
                return;
            }

            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            HttpResponseMessage response = Networking.HttpClient.PostAsync("https://r5r.org/eula", content).Result;

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInfo(Logger.Source.Launcher, "Successfully got EULA");
                EULAData euladata = JsonConvert.DeserializeObject<EULAData>(response.Content.ReadAsStringAsync().Result);
                EULATextBox.Text = euladata.data.contents;
            }
            else
            {
                Logger.LogError(Logger.Source.Launcher, "Failed to get EULA");
            }
        }

        private void acknowledge_Click(object sender, RoutedEventArgs e)
        {
            SetBranch.EULAAccepted(true);
            Task.Run(() => Install.Start());
            Managers.App.HideEULA();
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            Managers.App.HideEULA();
        }
    }

    public class Data
    {
        public int version { get; set; }
        public string lang { get; set; }
        public string contents { get; set; }
        public DateTime modified { get; set; }
        public string language { get; set; }
    }

    public class EULAData
    {
        public bool success { get; set; }
        public Data data { get; set; }
    }
}