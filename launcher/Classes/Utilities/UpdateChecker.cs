using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using static launcher.Classes.Utilities.Logger;
using System.IO;
using launcher.Classes.BranchUtils;
using static launcher.Classes.Global.References;
using launcher.Classes.Global;

namespace launcher.Classes.Utilities
{
    public static class UpdateChecker
    {
        private static bool iqnoredLauncherUpdate = false;

        public static async Task Start()
        {
            if (!AppState.IsOnline)
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
                        LogInfo(Source.UpdateChecker, $"Update for launcher is not available (latest version: {newServerConfig.launcherVersion})");
                    }

                    if (ShouldUpdateGame(newServerConfig))
                    {
                        HandleGameUpdate();
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
                response = await Networking.HttpClient.GetAsync(Launcher.CONFIG_URL);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ServerConfig>(responseString);
            }
            catch (HttpRequestException ex)
            {
                LogError(Source.UpdateChecker, $"HTTP Request Failed: {ex.Message}");
                return null;
            }
            finally
            {
                response?.Dispose();
            }
        }

        private static bool IsNewVersion(string version, string newVersion)
        {
            var currentParts = version.Split('.').Select(int.Parse).ToArray();
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

            return false;
        }

        private static bool ShouldUpdateLauncher(ServerConfig newServerConfig)
        {
            return !iqnoredLauncherUpdate &&
                !AppState.IsInstalling &&
                newServerConfig.launcherallowUpdates &&
                IsNewVersion(Launcher.VERSION, newServerConfig.launcherVersion);
        }

        private static bool ShouldUpdateGame(ServerConfig newServerConfig)
        {
            return !AppState.IsInstalling &&
                   newServerConfig.branches[GetBranch.Index()].allow_updates &&
                   Configuration.LauncherConfig != null &&
                   !GetBranch.IsLocalBranch() &&
                   GetBranch.Installed() &&
                   GetBranch.LocalVersion() != GetBranch.ServerVersion();
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
            if (!File.Exists($"{Launcher.PATH}\\launcher_data\\updater.exe"))
            {
                LogError(Source.UpdateChecker, "Self updater not found");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"\" \"{Launcher.PATH}\\launcher_data\\updater.exe\""
            };

            Process.Start(startInfo);

            Environment.Exit(0);
        }

        private static void HandleGameUpdate()
        {
            if (GetBranch.IsLocalBranch())
                return;

            if (GetBranch.UpdateAvailable())
                return;

            appDispatcher.Invoke(() =>
            {
                SetBranch.UpdateAvailable(true);
                Update_Button.Visibility = Visibility.Visible;
            });
        }
    }
}