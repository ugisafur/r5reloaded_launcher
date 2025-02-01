using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static launcher.Global.Logger;

namespace launcher
{
    /// <summary>
    /// Interaction logic for StatusPopup.xaml
    /// </summary>
    public partial class Popup_Services : UserControl
    {
        private const int refresh_interval = 30;
        private const string website_url = "https://r5reloaded.com/";
        private const string ms_url = "https://r5r.org/";
        private const string cdn_url = "https://cdn.r5r.org/launcher/config.json";

        public Popup_Services()
        {
            InitializeComponent();
        }

        private void Border_Loaded(object sender, RoutedEventArgs e)
        {
        }

        public async void StartStatusTimer()
        {
            Task.Run(() => GetStatusInfo());

            int current_time = 0;

            while (true)
            {
                await Task.Delay(1000);
                current_time++;

                Dispatcher.Invoke(() =>
                {
                    LastUpdate.Text = $"Last Update: {current_time} seconds ago";
                });

                if (current_time >= refresh_interval)
                {
                    Task.Run(() => GetStatusInfo());
                    current_time = 0;
                }
            }
        }

        private async void GetStatusInfo()
        {
            bool isWebsiteUP = await IsUrlUp(website_url);
            bool isMSUP = await IsUrlUp(ms_url);
            bool isCDNUP = await IsUrlUp(cdn_url);

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
            {
                LogError(Source.API, "Master Server is down.");
                return;
            }

            string serverlist = await SendPostRequestAsync($"{ms_url}servers", "{}");

            if (string.IsNullOrEmpty(serverlist))
            {
                LogError(Source.API, "Failed to get server list from API.");
                return;
            }

            GameServerList game_server_list = JsonConvert.DeserializeObject<GameServerList>(serverlist);

            if (!game_server_list.success)
            {
                LogError(Source.API, "Failed to get server list from API.");
                return;
            }

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
                LogError(Source.API, $"URL is down or unreachable: {url}");
            }
            catch (TaskCanceledException)
            {
                // Handle request timeout
                LogError(Source.API, $"Request timed out: {url}");
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                LogError(Source.API, $"An error occurred: {ex.Message}");
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

        private void moreInfo_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("cmd", $"/c start {website_url}") { CreateNoWindow = true });
        }
    }

    public class GameServerList
    {
        public bool success { get; set; }
        public List<Server> servers { get; set; }
    }

    public class Server
    {
        public string maxPlayers { get; set; }
        public string port { get; set; }
        public string checksum { get; set; }
        public string name { get; set; }
        public string ip { get; set; }
        public string description { get; set; }
        public string hidden { get; set; }
        public string playerCount { get; set; }
        public string playlist { get; set; }
        public string key { get; set; }
        public string region { get; set; }
        public string map { get; set; }
    }
}