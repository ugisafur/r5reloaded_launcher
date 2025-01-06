using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;

namespace launcher
{
    /// <summary>
    /// The UpdateChecker class is responsible for periodically checking for updates to both the launcher and the game.
    /// It fetches the latest configuration from a remote server, determines if an update is necessary, and handles the update process.
    /// This class uses asynchronous operations to perform network requests and updates the UI using a Dispatcher.
    /// </summary>
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
            Global.updateCheckLoop = true;

            while (Global.updateCheckLoop)
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

                await Task.Delay(30000);
            }
        }

        private async Task<ServerConfig> GetServerConfigAsync()
        {
            HttpResponseMessage response = null;
            try
            {
                response = await Global.client.Value.GetAsync(ConfigUrl);
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

        private bool IsNewVersion(string currentVersion, string newVersion)
        {
            var currentParts = currentVersion.Split('.').Select(int.Parse).ToArray();
            var newParts = newVersion.Split('.').Select(int.Parse).ToArray();

            for (int i = 0; i < Math.Max(currentParts.Length, newParts.Length); i++)
            {
                int currentPart = i < currentParts.Length ? currentParts[i] : 0;
                int newPart = i < newParts.Length ? newParts[i] : 0;

                if (currentPart < newPart)
                    return true;
                if (currentPart > newPart)
                    return false;
            }

            return false; // Versions are the same
        }

        private bool ShouldUpdateLauncher(ServerConfig newServerConfig)
        {
            return !iqnoredLauncherUpdate && !Global.isInstalling && Global.isInstalled && IsNewVersion(Global.launcherVersion, newServerConfig.launcherVersion);
        }

        private bool ShouldUpdateGame(ServerConfig newServerConfig)
        {
            return !Global.isInstalling &&
                   newServerConfig.allowUpdates &&
                   Global.launcherConfig != null &&
                   newServerConfig.branches[0].currentVersion != Global.launcherConfig.currentUpdateVersion;
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
            UpdateLauncher();
        }

        private static void UpdateLauncher()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"\" \"{Global.launcherPath}\\bin\\selfupdater.exe\""
            };

            // Start the new process via cmd
            Process.Start(startInfo);

            Environment.Exit(0);
        }

        private void HandleGameUpdate(ServerConfig newServerConfig)
        {
            _dispatcher.Invoke(() =>
            {
                Global.serverConfig = newServerConfig;
                Global.updateRequired = true;
                Global.updateCheckLoop = false;
                ControlReferences.cmbBranch.ItemsSource = Utilities.SetupGameBranches();
                ControlReferences.cmbBranch.SelectedIndex = 0;
            });
        }
    }
}