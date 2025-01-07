using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

namespace launcher
{
    /// <summary>
    /// Interaction logic for StatusPopup.xaml
    /// </summary>
    public partial class StatusPopup : UserControl
    {
        public StatusPopup()
        {
            InitializeComponent();
        }

        private void Border_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() => GetStatusInfo());
        }

        private async void GetStatusInfo()
        {
            bool isWebsiteUP = await IsUrlUp("https://r5reloaded.com/");
            Logger.Log(Logger.Type.Info, Logger.Source.API, "Getting website status");

            bool isMSUP = await IsUrlUp("https://r5r.org/");
            Logger.Log(Logger.Type.Info, Logger.Source.API, "Getting master server status");

            bool isCDNUP = await IsUrlUp("https://cdn.r5r.org/launcher/config.json");
            Logger.Log(Logger.Type.Info, Logger.Source.API, "Getting CDN status");

            Dispatcher.Invoke(() =>
            {
                Brush upBrush = new SolidColorBrush(Color.FromArgb(255, 141, 243, 187));
                Brush downBrush = new SolidColorBrush(Color.FromArgb(255, 243, 141, 141));

                WebsiteStatusBG.Background = isWebsiteUP ? upBrush : downBrush;
                MSStatusBG.Background = isMSUP ? upBrush : downBrush;
                CDNStatusBG.Background = isCDNUP ? upBrush : downBrush;

                lblWebsiteStatus.Text = isWebsiteUP ? "Operational" : "Non-Operational";
                lblMSStatus.Text = isMSUP ? "Operational" : "Non-Operational";
                lblCNDStatus.Text = isCDNUP ? "Operational" : "Non-Operational";
            });

            if (!isMSUP)
                return;

            string serverlist = await SendPostRequestAsync("https://r5r.org/servers", "{}");

            Logger.Log(Logger.Type.Info, Logger.Source.API, "Getting total servers and players");

            if (string.IsNullOrEmpty(serverlist))
            {
                Console.WriteLine("Failed to get server list");
                return;
            }

            GameServerList game_server_list = JsonConvert.DeserializeObject<GameServerList>(serverlist);

            if (!game_server_list.success)
                return;

            int total_players = 0;

            foreach (var server in game_server_list.servers)
                total_players += int.Parse(server.playerCount);

            Dispatcher.Invoke(() =>
            {
                lblPlayersCount.Text = total_players.ToString();
                lblServerCount.Text = game_server_list.servers.Count.ToString();
            });
        }

        private static async Task<bool> IsUrlUp(string url)
        {
            try
            {
                using HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5); // Set a timeout for the request

                HttpResponseMessage response = await client.GetAsync(url);
                return response.IsSuccessStatusCode; // Returns true if status code is 2xx
            }
            catch (HttpRequestException)
            {
                // Handle HTTP-related errors (e.g., DNS failure, connection issues)
                Logger.Log(Logger.Type.Error, Logger.Source.API, $"URL is down or unreachable: {url}");
            }
            catch (TaskCanceledException)
            {
                // Handle request timeout
                Logger.Log(Logger.Type.Error, Logger.Source.API, $"Request timed out: {url}");
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                Logger.Log(Logger.Type.Error, Logger.Source.API, $"An error occurred: {ex.Message}");
            }

            return false; // URL is down or unreachable
        }

        public async Task<string> SendPostRequestAsync(string url, string jsonContent)
        {
            using (HttpClient client = new HttpClient())
            {
                // Set headers if needed (optional)
                // Create the content to send in the POST request (in JSON format)
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Send the POST request and get the response
                HttpResponseMessage response = await client.PostAsync(url, content);

                // Ensure successful response
                response.EnsureSuccessStatusCode();

                // Read the JSON response as a string
                string responseJson = await response.Content.ReadAsStringAsync();
                return responseJson;
            }
        }
    }
}