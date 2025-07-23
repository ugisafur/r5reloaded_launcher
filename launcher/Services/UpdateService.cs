using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using static launcher.Utils.Logger;
using System.IO;
using static launcher.Core.UiReferences;
using launcher.Core;
using launcher.Core.Models;
using launcher.Services.Models;
using launcher.Configuration;

using static launcher.Core.Application;

namespace launcher.Services
{
    public static class UpdateService
    {
        public static bool otherPopupsOpened = false;

        public static bool launcherPopupOpened = false;
        public static bool? wantsToUpdate = null;

        public static bool iqnoredLauncherUpdate = false;
        public static bool checkForUpdatesOveride = false;

        public static async Task Start()
        {
            if (!AppState.IsOnline)
                return;

            if (string.IsNullOrEmpty((string)IniSettings.Get(IniSettings.Vars.Launcher_Version)) && (string)IniSettings.Get(IniSettings.Vars.Launcher_Version) == Launcher.VERSION)
            {
                IniSettings.Set(IniSettings.Vars.Launcher_Version, Launcher.VERSION);
            }

            LogInfo(LogSource.UpdateChecker, "Update worker started");

            while (true)
            {
                LogInfo(LogSource.UpdateChecker, "Checking for updates");

                try
                {
                    var newServerConfig = await GetServerConfigAsync();
                    var newGithubConfig = await GetGithubConfigAsync();
                    if (newServerConfig == null || newServerConfig.branches == null)
                    {
                        LogError(LogSource.UpdateChecker, "Failed to fetch new server config");
                        continue;
                    }

                    if (!otherPopupsOpened && ShouldUpdateLauncher(newServerConfig, newGithubConfig) && newGithubConfig != null && newGithubConfig.Count > 0)
                    {
                        HandleLauncherUpdate();
                    }
                    else
                    {
                        string version = (bool)IniSettings.Get(IniSettings.Vars.Nightly_Builds) ? (string)IniSettings.Get(IniSettings.Vars.Launcher_Version) : Launcher.ServerConfig.launcherVersion;
                        string message = iqnoredLauncherUpdate ? "Update for launcher is available but user iqnored update" : "Update for launcher is not available";
                        LogInfo(LogSource.UpdateChecker, $"{message} (latest version: {version})");
                    }

                    if (ShouldUpdateGame(newServerConfig) && newServerConfig.branches.Count > 0)
                    {
                        HandleGameUpdate();
                    }
                }
                catch (HttpRequestException ex)
                {
                    LogException($"HTTP Request Failed", LogSource.UpdateChecker, ex);
                }
                catch (JsonSerializationException ex)
                {
                    LogException($"JSON Deserialization Failed", LogSource.UpdateChecker, ex);
                }
                catch (Exception ex)
                {
                    LogException($"Unexpected Error", LogSource.UpdateChecker, ex);
                }

                await WaitTime(5);
            }
        }

        private static async Task WaitTime(double Minutes)
        {
            DateTime start = DateTime.Now;
            checkForUpdatesOveride = false;

            while (true)
            {
                if (checkForUpdatesOveride)
                {
                    break;
                }

                if ((DateTime.Now - start).TotalMinutes >= Minutes)
                {
                    break;
                }

                await Task.Delay(1000);
            }
        }

        private static async Task<ServerConfig> GetServerConfigAsync()
        {
            HttpResponseMessage response = null;
            try
            {
                response = await NetworkHealthService.HttpClient.GetAsync(Launcher.CONFIG_URL);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ServerConfig>(responseString);
            }
            catch (HttpRequestException ex)
            {
                LogException($"HTTP Request Failed", LogSource.UpdateChecker, ex);
                return null;
            }
            finally
            {
                response?.Dispose();
            }
        }

        private static async Task<List<GithubItems>> GetGithubConfigAsync()
        {
            HttpResponseMessage response = null;
            try
            {
                NetworkHealthService.HttpClient.DefaultRequestHeaders.Add("User-Agent", "request");
                response = await NetworkHealthService.HttpClient.GetAsync(Launcher.GITHUB_API_URL);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();

                NetworkHealthService.HttpClient.DefaultRequestHeaders.Remove("User-Agent");

                return JsonConvert.DeserializeObject<List<GithubItems>>(responseString);
            }
            catch (HttpRequestException ex)
            {
                LogException($"HTTP Request Failed", LogSource.UpdateChecker, ex);
                return null;
            }
            finally
            {
                response?.Dispose();
            }
        }

        private static bool IsNewVersion(string version, string newVersion)
        {
            if (((string)IniSettings.Get(IniSettings.Vars.Launcher_Version)).Contains("nightly"))
            {
                return true;
            }

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

        private static bool IsNewNightlyVersion(string version, List<GithubItems> newGithubConfig)
        {
            return GetLatestNightlyTag(newGithubConfig) != version;
        }

        private static string GetLatestNightlyTag(List<GithubItems> newGithubConfig)
        {
            if (newGithubConfig.Count > 0)
            {
                return newGithubConfig[0].tag_name;
            }

            return "";
        }

        private static bool ShouldUpdateLauncher(ServerConfig newServerConfig, List<GithubItems> newGithubConfig)
        {
            if ((bool)IniSettings.Get(IniSettings.Vars.Nightly_Builds))
            {
                newGithubConfig = newGithubConfig.Where(release => release.prerelease && release.tag_name.StartsWith("nightly")).ToList();

                if (!iqnoredLauncherUpdate && !AppState.IsInstalling && IsNewNightlyVersion((string)IniSettings.Get(IniSettings.Vars.Launcher_Version), newGithubConfig))
                {
                    appDispatcher.BeginInvoke(() =>
                    {
                        LauncherUpdate_Control.SetUpdateText($"A new nightly version of the launcher is available. Would you like to update now?\n\nCurrent Version: {Launcher.VERSION}\nNew Version: {newGithubConfig[0].tag_name}", null);
                        ShowLauncherUpdatePopup();
                    });
                    
                    launcherPopupOpened = true;

                    while (launcherPopupOpened)
                    {
                        Task.Delay(250);
                    }

                    if(wantsToUpdate == null)
                        return false;

                    if (wantsToUpdate == false)
                    {
                        iqnoredLauncherUpdate = true;
                        return false;
                    }

                    if (wantsToUpdate == true)
                    {
                        IniSettings.Set(IniSettings.Vars.Launcher_Version, newGithubConfig[0].tag_name);
                        return true;
                    }
                }

                return false;
            }

            if (!iqnoredLauncherUpdate && !AppState.IsInstalling && newServerConfig.allowUpdates && IsNewVersion(Launcher.VERSION, newServerConfig.launcherVersion))
            {
                appDispatcher.BeginInvoke(() =>
                {
                    string postUpdateMessage = newServerConfig.forceUpdates ? "This update is required." : "Would you like to update now?";
                    LauncherUpdate_Control.SetUpdateText($"A new version of the launcher is available. {postUpdateMessage}\n\nCurrent Version: {Launcher.VERSION}\nNew Version: {newServerConfig.launcherVersion}", newServerConfig);
                    ShowLauncherUpdatePopup();
                });

                launcherPopupOpened = true;

                while (launcherPopupOpened)
                {
                    Task.Delay(250);
                }

                if (wantsToUpdate == null)
                    return false;

                if (wantsToUpdate == false)
                {
                    iqnoredLauncherUpdate = true;
                    return false;
                }

                if (wantsToUpdate == true)
                {
                    IniSettings.Set(IniSettings.Vars.Launcher_Version, newServerConfig.launcherVersion);
                    return true;
                }
            }

            return false;
        }

        private static bool ShouldUpdateGame(ServerConfig newServerConfig)
        {
            if (Launcher.LauncherConfig == null)
                return false;

            if (newServerConfig.branches.Count == 0)
                return false;

            if(!AppState.IsOnline)
                return false;

            if (AppState.IsInstalling)
                return false;

            if (BranchService.IsLocal())
                return false;

            if(!BranchService.IsInstalled())
                return false;

            if (!newServerConfig.branches[BranchService.GetCurrentIndex()].allow_updates)
                return false;

            if(BranchService.GetLocalVersion() == BranchService.GetServerVersion())
                return false;

            return true;
        }

        private static void HandleLauncherUpdate()
        {
            LogInfo(LogSource.UpdateChecker, "Updating launcher...");
            UpdateLauncher();
        }

        private static void UpdateLauncher()
        {
            if (!File.Exists($"{Launcher.PATH}\\launcher_data\\updater.exe"))
            {
                LogError(LogSource.UpdateChecker, "Self updater not found");
                return;
            }

            string extraArgs = (bool)IniSettings.Get(IniSettings.Vars.Nightly_Builds) ? " -nightly" : "";

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"\" \"{Launcher.PATH}\\launcher_data\\updater.exe\"{extraArgs}"
            };

            Process.Start(startInfo);

            Environment.Exit(0);
        }

        private static void HandleGameUpdate()
        {
            if (BranchService.IsLocal())
                return;

            if (BranchService.IsUpdateAvailable())
                return;

            appDispatcher.Invoke(() =>
            {
                BranchService.SetUpdateAvailable(true);
                Update_Button.Visibility = Visibility.Visible;
            });
        }
    }
}