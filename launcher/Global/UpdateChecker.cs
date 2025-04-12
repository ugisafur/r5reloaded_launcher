using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using static launcher.Global.Logger;
using System.IO;
using static launcher.Global.References;
using launcher.Game;

namespace launcher.Global
{
    public static class UpdateChecker
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

            if (string.IsNullOrEmpty((string)Ini.Get(Ini.Vars.Launcher_Version)) && (string)Ini.Get(Ini.Vars.Launcher_Version) == Launcher.VERSION)
            {
                Ini.Set(Ini.Vars.Launcher_Version, Launcher.VERSION);
            }

            LogInfo(Source.UpdateChecker, "Update worker started");

            while (true)
            {
                LogInfo(Source.UpdateChecker, "Checking for updates");

                try
                {
                    var newServerConfig = await GetServerConfigAsync();
                    var newGithubConfig = await GetGithubConfigAsync();
                    if (newServerConfig == null || newServerConfig.branches == null)
                    {
                        LogError(Source.UpdateChecker, "Failed to fetch new server config");
                        continue;
                    }

                    if (!otherPopupsOpened && ShouldUpdateLauncher(newServerConfig, newGithubConfig) && newGithubConfig != null && newGithubConfig.Count > 0)
                    {
                        HandleLauncherUpdate();
                    }
                    else
                    {
                        string version = (bool)Ini.Get(Ini.Vars.Nightly_Builds) ? (string)Ini.Get(Ini.Vars.Launcher_Version) : Launcher.ServerConfig.launcherVersion;
                        string message = iqnoredLauncherUpdate ? "Update for launcher is available but user iqnored update" : "Update for launcher is not available";
                        LogInfo(Source.UpdateChecker, $"{message} (latest version: {version})");
                    }

                    if (ShouldUpdateGame(newServerConfig) && newServerConfig.branches.Count > 0)
                    {
                        HandleGameUpdate();
                    }
                }
                catch (HttpRequestException ex)
                {
                    LogException($"HTTP Request Failed", Source.UpdateChecker, ex);
                }
                catch (JsonSerializationException ex)
                {
                    LogException($"JSON Deserialization Failed", Source.UpdateChecker, ex);
                }
                catch (Exception ex)
                {
                    LogException($"Unexpected Error", Source.UpdateChecker, ex);
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
                response = await Networking.HttpClient.GetAsync(Launcher.CONFIG_URL);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ServerConfig>(responseString);
            }
            catch (HttpRequestException ex)
            {
                LogException($"HTTP Request Failed", Source.UpdateChecker, ex);
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
                Networking.HttpClient.DefaultRequestHeaders.Add("User-Agent", "request");
                response = await Networking.HttpClient.GetAsync(Launcher.GITHUB_API_URL);
                response.EnsureSuccessStatusCode();
                var responseString = await response.Content.ReadAsStringAsync();

                Networking.HttpClient.DefaultRequestHeaders.Remove("User-Agent");

                return JsonConvert.DeserializeObject<List<GithubItems>>(responseString);
            }
            catch (HttpRequestException ex)
            {
                LogException($"HTTP Request Failed", Source.UpdateChecker, ex);
                return null;
            }
            finally
            {
                response?.Dispose();
            }
        }

        private static bool IsNewVersion(string version, string newVersion)
        {
            if (((string)Ini.Get(Ini.Vars.Launcher_Version)).Contains("nightly"))
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
            if ((bool)Ini.Get(Ini.Vars.Nightly_Builds))
            {
                newGithubConfig = newGithubConfig.Where(release => release.prerelease && release.tag_name.StartsWith("nightly")).ToList();

                if (!iqnoredLauncherUpdate && !AppState.IsInstalling && IsNewNightlyVersion((string)Ini.Get(Ini.Vars.Launcher_Version), newGithubConfig))
                {
                    appDispatcher.BeginInvoke(() =>
                    {
                        LauncherUpdate_Control.SetUpdateText("A new nightly version of the launcher is available. Would you like to update now?");
                        Managers.App.ShowLauncherUpdatePopup();
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
                        Ini.Set(Ini.Vars.Launcher_Version, newGithubConfig[0].tag_name);
                        return true;
                    }
                }

                return false;
            }

            if (!iqnoredLauncherUpdate && !AppState.IsInstalling && newServerConfig.launcherallowUpdates && IsNewVersion(Launcher.VERSION, newServerConfig.launcherVersion))
            {
                appDispatcher.BeginInvoke(() =>
                {
                    LauncherUpdate_Control.SetUpdateText("A new version of the launcher is available. Would you like to update now?");
                    Managers.App.ShowLauncherUpdatePopup();
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
                    Ini.Set(Ini.Vars.Launcher_Version, newServerConfig.launcherVersion);
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

            if (GetBranch.IsLocalBranch())
                return false;

            if(!GetBranch.Installed())
                return false;

            if (!newServerConfig.branches[GetBranch.Index()].allow_updates)
                return false;

            if(GetBranch.LocalVersion() == GetBranch.ServerVersion())
                return false;

            return true;
        }

        private static void HandleLauncherUpdate()
        {
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

            string extraArgs = (bool)Ini.Get(Ini.Vars.Nightly_Builds) ? " -nightly" : "";

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

    public class Asset
    {
        public string url { get; set; }
        public int id { get; set; }
        public string node_id { get; set; }
        public string name { get; set; }
        public string label { get; set; }
        public Uploader uploader { get; set; }
        public string content_type { get; set; }
        public string state { get; set; }
        public int size { get; set; }
        public int download_count { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public string browser_download_url { get; set; }
    }

    public class GitAuthor
    {
        public string login { get; set; }
        public int id { get; set; }
        public string node_id { get; set; }
        public string avatar_url { get; set; }
        public string gravatar_id { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string followers_url { get; set; }
        public string following_url { get; set; }
        public string gists_url { get; set; }
        public string starred_url { get; set; }
        public string subscriptions_url { get; set; }
        public string organizations_url { get; set; }
        public string repos_url { get; set; }
        public string events_url { get; set; }
        public string received_events_url { get; set; }
        public string type { get; set; }
        public string user_view_type { get; set; }
        public bool site_admin { get; set; }
    }

    public class GithubItems
    {
        public string url { get; set; }
        public string assets_url { get; set; }
        public string upload_url { get; set; }
        public string html_url { get; set; }
        public int id { get; set; }
        public GitAuthor author { get; set; }
        public string node_id { get; set; }
        public string tag_name { get; set; }
        public string target_commitish { get; set; }
        public string name { get; set; }
        public bool draft { get; set; }
        public bool prerelease { get; set; }
        public DateTime created_at { get; set; }
        public DateTime published_at { get; set; }
        public List<Asset> assets { get; set; }
        public string tarball_url { get; set; }
        public string zipball_url { get; set; }
        public string body { get; set; }
    }

    public class Uploader
    {
        public string login { get; set; }
        public int id { get; set; }
        public string node_id { get; set; }
        public string avatar_url { get; set; }
        public string gravatar_id { get; set; }
        public string url { get; set; }
        public string html_url { get; set; }
        public string followers_url { get; set; }
        public string following_url { get; set; }
        public string gists_url { get; set; }
        public string starred_url { get; set; }
        public string subscriptions_url { get; set; }
        public string organizations_url { get; set; }
        public string repos_url { get; set; }
        public string events_url { get; set; }
        public string received_events_url { get; set; }
        public string type { get; set; }
        public string user_view_type { get; set; }
        public bool site_admin { get; set; }
    }
}