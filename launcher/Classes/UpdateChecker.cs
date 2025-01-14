using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using static launcher.Global;
using static launcher.ControlReferences;
using static launcher.Logger;
using System.IO;
using System.Text.RegularExpressions;

namespace launcher
{
    /// <summary>
    /// The UpdateChecker class is responsible for periodically checking for updates to both the launcher and the game.
    /// It fetches the latest configuration from a remote server, determines if an update is necessary, and handles the update process.
    /// This class uses asynchronous operations to perform network requests and updates the UI using a Dispatcher.
    /// </summary>
    public static class UpdateChecker
    {
        private static bool iqnoredLauncherUpdate = false;

        public static async Task Start()
        {
            if (!IS_ONLINE)
                return;

            LogInfo(Source.UpdateChecker, "Update worker started");

            while (true)
            {
                LogInfo(Source.UpdateChecker, "Checking for updates");

                try
                {
                    var newServerConfig = await GetServerConfigAsync();
                    if (newServerConfig == null)
                    {
                        LogError(Source.UpdateChecker, "Failed to fetch new server config");
                        continue;
                    }

                    if (ShouldUpdateLauncher(newServerConfig))
                    {
                        HandleLauncherUpdate();
                    }
                    else
                    {
                        LogInfo(Source.UpdateChecker, $"Update for version {LAUNCHER_VERSION} is not available (latest version: {LAUNCHER_VERSION})");
                    }

                    if (ShouldUpdateGame(newServerConfig))
                    {
                        HandleGameUpdate(newServerConfig);
                    }
                }
                catch (HttpRequestException ex)
                {
                    LogError(Source.UpdateChecker, $"HTTP Request Failed: {ex.Message}");
                }
                catch (JsonSerializationException ex)
                {
                    LogError(Source.UpdateChecker, $"JSON Deserialization Failed: {ex.Message}");
                }
                catch (Exception ex)
                {
                    LogError(Source.UpdateChecker, $"Unexpected Error: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        }

        private static async Task<ServerConfig> GetServerConfigAsync()
        {
            HttpResponseMessage response = null;
            try
            {
                response = await HTTP_CLIENT.GetAsync(SERVER_CONFIG_URL);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ServerConfig>(responseString);
            }
            catch (HttpRequestException ex)
            {
                LogError(Source.UpdateChecker, $"HTTP Request Failed: {ex.Message}");
                return null; //Indicate failure to the caller
            }
            finally
            {
                response?.Dispose();
            }
        }

        private static bool IsNewVersion(string currentVersion, string newVersion)
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

        private static bool ShouldUpdateLauncher(ServerConfig newServerConfig)
        {
            return !iqnoredLauncherUpdate && !IS_INSTALLING && IsNewVersion(LAUNCHER_VERSION, newServerConfig.launcherVersion);
        }

        private static bool ShouldUpdateGame(ServerConfig newServerConfig)
        {
            return !IS_INSTALLING &&
                   newServerConfig.allowUpdates &&
                   LAUNCHER_CONFIG != null &&
                   !SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].is_local_branch &&
                   Ini.Get(SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].branch, "Is_Installed", false) &&
                   newServerConfig.branches[Utilities.GetCmbBranchIndex()].currentVersion != Utilities.GetBranchVersion();
        }

        private static void HandleLauncherUpdate()
        {
            var messageBoxResult = MessageBox.Show("A new version of the launcher is available. Would you like to update now?", "Launcher Update", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (messageBoxResult == MessageBoxResult.No)
            {
                iqnoredLauncherUpdate = true;
                return;
            }

            LogInfo(Source.UpdateChecker, "Updating launcher...");
            UpdateLauncher();
        }

        private static void UpdateLauncher()
        {
            if (!File.Exists($"{LAUNCHER_PATH}\\launcher_data\\selfupdater.exe"))
            {
                LogError(Source.UpdateChecker, "Self updater not found");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"\" \"{LAUNCHER_PATH}\\launcher_data\\selfupdater.exe\""
            };

            // Start the new process via cmd
            Process.Start(startInfo);

            Environment.Exit(0);
        }

        private static void HandleGameUpdate(ServerConfig newServerConfig)
        {
            if (SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].is_local_branch)
                return;

            if (SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].update_available)
                return;

            appDispatcher.Invoke(() =>
            {
                SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].currentVersion = newServerConfig.branches[Utilities.GetCmbBranchIndex()].currentVersion;
                SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].lastVersion = newServerConfig.branches[Utilities.GetCmbBranchIndex()].lastVersion;
                SERVER_CONFIG.branches[Utilities.GetCmbBranchIndex()].update_available = true;
                Update_Button.Visibility = Visibility.Visible;
            });
        }
    }
}