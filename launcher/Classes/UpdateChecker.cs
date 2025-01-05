using Newtonsoft.Json;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;

namespace launcher
{
    public class UpdateChecker
    {
        private const string ConfigUrl = "https://cdn.r5r.org/launcher/config.json";
        private readonly Dispatcher _dispatcher;

        private bool iqnoredLauncherUpdate = false;

        public UpdateChecker(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task Start()
        {
            Helper.updateCheckLoop = true;

            while (Helper.updateCheckLoop)
            {
                Console.WriteLine("Checking for updates...");

                try
                {
                    var newServerConfig = await GetServerConfigAsync();
                    if (newServerConfig == null)
                    {
                        Console.WriteLine("Failed to fetch new server config.");
                        continue;
                    }

                    if (ShouldUpdateLauncher(newServerConfig))
                    {
                        HandleLauncherUpdate();
                    }
                    else if (ShouldUpdateGame(newServerConfig))
                    {
                        HandleGameUpdate(newServerConfig);
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Error during HTTP Request: {ex.Message}");
                }
                catch (JsonSerializationException ex)
                {
                    Console.WriteLine($"Error during JSON deserialization: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                }

                await Task.Delay(10000);
            }
        }

        private async Task<ServerConfig> GetServerConfigAsync()
        {
            HttpResponseMessage response = null;
            try
            {
                response = await Helper.client.GetAsync(ConfigUrl);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ServerConfig>(responseString);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP Request Failed: {ex.Message}");
                return null; //Indicate failure to the caller
            }
            finally
            {
                response?.Dispose();
            }
        }

        private bool ShouldUpdateLauncher(ServerConfig newServerConfig)
        {
            return !iqnoredLauncherUpdate && !Helper.isInstalling && Helper.isInstalled && Helper.IsNewVersion(Helper.launcherVersion, newServerConfig.launcherVersion);
        }

        private bool ShouldUpdateGame(ServerConfig newServerConfig)
        {
            return !Helper.isInstalling &&
                   newServerConfig.allowUpdates &&
                   Helper.launcherConfig != null &&
                   newServerConfig.branches[0].currentVersion != Helper.launcherConfig.currentUpdateVersion;
        }

        private void HandleLauncherUpdate()
        {
            var messageBoxResult = MessageBox.Show("A new version of the launcher is available. Would you like to update now?", "Launcher Update", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (messageBoxResult == MessageBoxResult.No)
            {
                iqnoredLauncherUpdate = true;
                return;
            }

            Console.WriteLine("Updating launcher...");
            Helper.UpdateLauncher();
        }

        private void HandleGameUpdate(ServerConfig newServerConfig)
        {
            _dispatcher.Invoke(() =>
            {
                Helper.serverConfig = newServerConfig;
                Helper.updateRequired = true;
                Helper.updateCheckLoop = false;
                Helper.App.cmbBranch.ItemsSource = Helper.SetupGameBranches();
                Helper.App.cmbBranch.SelectedIndex = 0;
            });
        }
    }
}