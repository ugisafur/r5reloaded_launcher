using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using launcher.Core.Models;
using launcher.GameLifecycle.Models;
using static launcher.Core.UiReferences;

namespace launcher.Services
{
    public static class ReleaseChannelService
    {
        public static List<ReleaseChannel> LocalFolders { get; } = [];

        public static int GetCurrentIndex() { return appDispatcher.Invoke(() => ReleaseChannel_Combobox.SelectedIndex); }

        public static ReleaseChannel GetCurrentBranch() { return Launcher.RemoteConfig.branches[GetCurrentIndex()]; }

        public static bool IsEnabled(ReleaseChannel channel = null) { return channel != null ? channel.enabled : GetCurrentBranch().enabled; }
        public static bool AreUpdatesAllowed(ReleaseChannel channel = null) { return channel != null ? channel.allow_updates : GetCurrentBranch().allow_updates; }
        public static bool IsLocal(ReleaseChannel channel = null) { return channel != null ? channel.is_local_branch : GetCurrentBranch().is_local_branch; }
        public static bool IsUpdateAvailable(ReleaseChannel channel = null) { return channel != null ? channel.update_available : GetCurrentBranch().update_available; }
        public static bool IsEULAAccepted(ReleaseChannel channel = null) { return channel != null ? (bool)SettingsService.Get(channel.branch, "EULA_Accepted", false) : (bool)SettingsService.Get(GetCurrentBranch().branch, "EULA_Accepted", false); }
        public static bool ShouldDownloadHDTextures(ReleaseChannel channel = null) { return channel != null ? (bool)SettingsService.Get(channel.branch, "Download_HD_Textures", false) : (bool)SettingsService.Get(GetCurrentBranch().branch, "Download_HD_Textures", false); }
        public static bool IsInstalled(ReleaseChannel channel = null) { return channel != null ? (bool)SettingsService.Get(channel.branch, "Is_Installed", false) : (bool)SettingsService.Get(GetCurrentBranch().branch, "Is_Installed", false); }
        public static bool DoesExeExist(ReleaseChannel channel = null) { return channel != null ? System.IO.Directory.Exists(GetDirectory(channel)) && File.Exists(Path.Combine(GetDirectory(channel), "r5apex.exe")) : System.IO.Directory.Exists(GetDirectory()) && File.Exists(Path.Combine(GetDirectory(), "r5apex.exe")); }

        public static string GetLocalVersion(ReleaseChannel channel = null) { return channel != null ? (string)SettingsService.Get(channel.branch, "Version", "") : (string)SettingsService.Get(GetCurrentBranch().branch, "Version", ""); }
        public static string GetServerComboVersion(ReleaseChannel channel) { return channel.is_local_branch ? "Local Install" : ApiService.GetGameVersion(channel.game_url); }
        public static string GetServerVersion() { return ApiService.GetGameVersion(GetCurrentBranch().game_url); }
        public static string GetDirectory(ReleaseChannel channel = null) { return channel != null ? Path.Combine((string)SettingsService.Get(SettingsService.Vars.Library_Location), "R5R Library", GetName(true, channel)) : Path.Combine((string)SettingsService.Get(SettingsService.Vars.Library_Location), "R5R Library", GetName()); }
        public static string GetDediURL(ReleaseChannel channel = null) { return channel != null ? channel.dedi_url : GetCurrentBranch().dedi_url; }
        public static string GetName(bool uppercase = true, ReleaseChannel channel = null) { return channel != null ? (uppercase ? channel.branch.ToUpper(new CultureInfo("en-US")) : channel.branch) : (uppercase ? GetCurrentBranch().branch.ToUpper(new CultureInfo("en-US")) : GetCurrentBranch().branch); }
        public static string GetGameURL(ReleaseChannel channel = null) { return channel != null ? channel.game_url : GetCurrentBranch().game_url; }

        public async static Task<string> GetBlogSlug(ReleaseChannel channel = null)
        {
            GameManifest GameManifest = await ApiService.GetGameManifestAsync(false);

            return GameManifest.blog_slug; 
        }

        public static void SetUpdateAvailable(bool value, ReleaseChannel channel = null)
        {
            if (channel != null)
                channel.update_available = value;
            else
                GetCurrentBranch().update_available = value;
        }

        public static void SetDownloadHDTextures(bool value, ReleaseChannel channel = null)
        {
            if (channel != null)
                SettingsService.Set(channel.branch, "Download_HD_Textures", value);
            else
                SettingsService.Set(GetName(false), "Download_HD_Textures", value);
        }

        public static void SetInstalled(bool value, ReleaseChannel channel = null)
        {
            if (channel != null)
                SettingsService.Set(channel.branch, "Is_Installed", value);
            else
                SettingsService.Set(GetName(false), "Is_Installed", value);
        }

        public static void SetVersion(string value, ReleaseChannel channel = null)
        {
            if (channel != null)
                SettingsService.Set(channel.branch, "Version", value);
            else
                SettingsService.Set(GetName(false), "Version", value);
        }

        public static void SetEULAAccepted(bool value, ReleaseChannel channel = null)
        {
            if (channel != null)
                SettingsService.Set(channel.branch, "EULA_Accepted", value);
            else
                SettingsService.Set(GetName(false), "EULA_Accepted", value);
        }
    }
} 