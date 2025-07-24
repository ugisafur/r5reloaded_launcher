using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using static launcher.Services.LoggerService;
using System.IO;
using static launcher.Core.UiReferences;
using launcher.Core.Models;
using launcher.Services.Models;

using static launcher.Core.AppControllerService;

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
            if (!Launcher.IsOnline)
                return;

            if (string.IsNullOrEmpty((string)SettingsService.Get(SettingsService.Vars.Launcher_Version)) && (string)SettingsService.Get(SettingsService.Vars.Launcher_Version) == Launcher.VERSION)
            {
                SettingsService.Set(SettingsService.Vars.Launcher_Version, Launcher.VERSION);
            }

            LogInfo(LogSource.UpdateChecker, "Update worker started");

            while (true)
            {
                LogInfo(LogSource.UpdateChecker, "Checking for updates");

                try
                {
                    var newRemoteConfig = await GetRemoteConfigAsync();
                    var newGithubConfig = await GetGithubConfigAsync();
                    if (newRemoteConfig == null || newRemoteConfig.branches == null)
                    {
                        LogError(LogSource.UpdateChecker, "Failed to fetch new server config");
                        continue;
                    }

                    if (!otherPopupsOpened && ShouldUpdateLauncher(newRemoteConfig, newGithubConfig) && newGithubConfig != null && newGithubConfig.Count > 0)
                    {
                        HandleLauncherUpdate();
                    }
                    else
                    {
                        string version = (bool)SettingsService.Get(SettingsService.Vars.Nightly_Builds) ? (string)SettingsService.Get(SettingsService.Vars.Launcher_Version) : Launcher.RemoteConfig.launcherVersion;
                        string message = iqnoredLauncherUpdate ? "Update for launcher is available but user iqnored update" : "Update for launcher is not available";
                        LogInfo(LogSource.UpdateChecker, $"{message} (latest version: {version})");
                    }

                    if (ShouldUpdateGame(newRemoteConfig) && newRemoteConfig.branches.Count > 0)
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

        private static async Task<RemoteConfig> GetRemoteConfigAsync()
        {
            HttpResponseMessage response = null;
            try
            {
                response = await NetworkHealthService.HttpClient.GetAsync(Launcher.CONFIG_URL);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<RemoteConfig>(responseString);
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
            if (((string)SettingsService.Get(SettingsService.Vars.Launcher_Version)).Contains("nightly"))
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

        private static bool ShouldUpdateLauncher(RemoteConfig newRemoteConfig, List<GithubItems> newGithubConfig)
        {
            if ((bool)SettingsService.Get(SettingsService.Vars.Nightly_Builds))
            {
                newGithubConfig = newGithubConfig.Where(release => release.prerelease && release.tag_name.StartsWith("nightly")).ToList();

                if (!iqnoredLauncherUpdate && !Launcher.IsInstalling && IsNewNightlyVersion((string)SettingsService.Get(SettingsService.Vars.Launcher_Version), newGithubConfig))
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
                        SettingsService.Set(SettingsService.Vars.Launcher_Version, newGithubConfig[0].tag_name);
                        return true;
                    }
                }

                return false;
            }

            if (!iqnoredLauncherUpdate && !Launcher.IsInstalling && newRemoteConfig.allowUpdates && IsNewVersion(Launcher.VERSION, newRemoteConfig.launcherVersion))
            {
                appDispatcher.BeginInvoke(() =>
                {
                    string postUpdateMessage = newRemoteConfig.forceUpdates ? "This update is required." : "Would you like to update now?";
                    LauncherUpdate_Control.SetUpdateText($"A new version of the launcher is available. {postUpdateMessage}\n\nCurrent Version: {Launcher.VERSION}\nNew Version: {newRemoteConfig.launcherVersion}", newRemoteConfig);
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
                    SettingsService.Set(SettingsService.Vars.Launcher_Version, newRemoteConfig.launcherVersion);
                    return true;
                }
            }

            return false;
        }

        private static bool ShouldUpdateGame(RemoteConfig newRemoteConfig)
        {
            if (Launcher.LauncherConfig == null)
                return false;

            if (newRemoteConfig.branches.Count == 0)
                return false;

            if(!Launcher.IsOnline)
                return false;

            if (Launcher.IsInstalling)
                return false;

            if (ReleaseChannelService.IsLocal())
                return false;

            if(!ReleaseChannelService.IsInstalled())
                return false;

            if (!newRemoteConfig.branches[ReleaseChannelService.GetCurrentIndex()].allow_updates)
                return false;

            if(ReleaseChannelService.GetLocalVersion() == ReleaseChannelService.GetServerVersion())
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

            string extraArgs = (bool)SettingsService.Get(SettingsService.Vars.Nightly_Builds) ? " -nightly" : "";

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
            if (ReleaseChannelService.IsLocal())
                return;

            if (ReleaseChannelService.IsUpdateAvailable())
                return;

            appDispatcher.Invoke(() =>
            {
                ReleaseChannelService.SetUpdateAvailable(true);
                Update_Button.Visibility = Visibility.Visible;
            });
        }
    }
}