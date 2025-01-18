using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static launcher.ControlReferences;

namespace launcher
{
    /// <summary>
    /// Interaction logic for EULAPopup.xaml
    /// </summary>
    public partial class EULAPopup : UserControl
    {
        public EULAPopup()
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

            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            HttpResponseMessage response = Networking.HttpClient.PostAsync("https://r5r.org/eula", content).Result;
            response.EnsureSuccessStatusCode();

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

        public string SendPostRequestAsync(string url, string jsonContent)
        {
            using (HttpClient client = new HttpClient())
            {
                // Set headers if needed (optional)
                // Create the content to send in the POST request (in JSON format)
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send the POST request and get the response
                HttpResponseMessage response = client.PostAsync(url, content).Result;

                // Ensure successful response
                response.EnsureSuccessStatusCode();

                // Read the JSON response as a string
                string responseJson = response.Content.ReadAsStringAsync().Result;
                return responseJson;
            }
        }

        private void acknowledge_Click(object sender, RoutedEventArgs e)
        {
            Ini.Set(Configuration.ServerConfig.branches[Utilities.GetCmbBranchIndex()].branch, "EULA_Accepted", true);
            Task.Run(() => GameInstall.Start());
            Utilities.HideEULA();
        }

        private void close_Click(object sender, RoutedEventArgs e)
        {
            Utilities.HideEULA();
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