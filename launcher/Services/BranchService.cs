using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using launcher.Core;
using launcher.Core.Models;
using launcher.Configuration;
using static launcher.Core.UiReferences;

namespace launcher.Services
{
    public static class BranchService
    {
        public static int GetCurrentIndex() { return appDispatcher.Invoke(() => Branch_Combobox.SelectedIndex); }

        public static Branch GetCurrentBranch() { return Launcher.ServerConfig.branches[GetCurrentIndex()]; }

        public static bool IsEnabled(Branch branch = null) { return branch != null ? branch.enabled : GetCurrentBranch().enabled; }
        public static bool AreUpdatesAllowed(Branch branch = null) { return branch != null ? branch.allow_updates : GetCurrentBranch().allow_updates; }
        public static bool IsLocal(Branch branch = null) { return branch != null ? branch.is_local_branch : GetCurrentBranch().is_local_branch; }
        public static bool IsUpdateAvailable(Branch branch = null) { return branch != null ? branch.update_available : GetCurrentBranch().update_available; }
        public static bool IsEULAAccepted(Branch branch = null) { return branch != null ? (bool)IniSettings.Get(branch.branch, "EULA_Accepted", false) : (bool)IniSettings.Get(GetCurrentBranch().branch, "EULA_Accepted", false); }
        public static bool ShouldDownloadHDTextures(Branch branch = null) { return branch != null ? (bool)IniSettings.Get(branch.branch, "Download_HD_Textures", false) : (bool)IniSettings.Get(GetCurrentBranch().branch, "Download_HD_Textures", false); }
        public static bool IsInstalled(Branch branch = null) { return branch != null ? (bool)IniSettings.Get(branch.branch, "Is_Installed", false) : (bool)IniSettings.Get(GetCurrentBranch().branch, "Is_Installed", false); }
        public static bool DoesExeExist(Branch branch = null) { return branch != null ? System.IO.Directory.Exists(GetDirectory(branch)) && File.Exists(Path.Combine(GetDirectory(branch), "r5apex.exe")) : System.IO.Directory.Exists(GetDirectory()) && File.Exists(Path.Combine(GetDirectory(), "r5apex.exe")); }

        public static string GetLocalVersion(Branch branch = null) { return branch != null ? (string)IniSettings.Get(branch.branch, "Version", "") : (string)IniSettings.Get(GetCurrentBranch().branch, "Version", ""); }
        public static string GetServerComboVersion(Branch branch) { return branch.is_local_branch ? "Local Install" : ApiClient.GetGameVersion(branch.game_url); }
        public static string GetServerVersion() { return ApiClient.GetGameVersion(GetCurrentBranch().game_url); }
        public static string GetDirectory(Branch branch = null) { return branch != null ? Path.Combine((string)IniSettings.Get(IniSettings.Vars.Library_Location), "R5R Library", GetName(true, branch)) : Path.Combine((string)IniSettings.Get(IniSettings.Vars.Library_Location), "R5R Library", GetName()); }
        public static string GetDediURL(Branch branch = null) { return branch != null ? branch.dedi_url : GetCurrentBranch().dedi_url; }
        public static string GetName(bool uppercase = true, Branch branch = null) { return branch != null ? (uppercase ? branch.branch.ToUpper(new CultureInfo("en-US")) : branch.branch) : (uppercase ? GetCurrentBranch().branch.ToUpper(new CultureInfo("en-US")) : GetCurrentBranch().branch); }
        public static string GetGameURL(Branch branch = null) { return branch != null ? branch.game_url : GetCurrentBranch().game_url; }

        public async static Task<string> GetBlogSlug(Branch branch = null)
        {
            GameFiles gameFiles = await ApiClient.GetGameFilesAsync(false);

            return gameFiles.blog_slug; 
        }

        public static void SetUpdateAvailable(bool value, Branch branch = null)
        {
            if (branch != null)
                branch.update_available = value;
            else
                GetCurrentBranch().update_available = value;
        }

        public static void SetDownloadHDTextures(bool value, Branch branch = null)
        {
            if (branch != null)
                IniSettings.Set(branch.branch, "Download_HD_Textures", value);
            else
                IniSettings.Set(GetName(false), "Download_HD_Textures", value);
        }

        public static void SetInstalled(bool value, Branch branch = null)
        {
            if (branch != null)
                IniSettings.Set(branch.branch, "Is_Installed", value);
            else
                IniSettings.Set(GetName(false), "Is_Installed", value);
        }

        public static void SetVersion(string value, Branch branch = null)
        {
            if (branch != null)
                IniSettings.Set(branch.branch, "Version", value);
            else
                IniSettings.Set(GetName(false), "Version", value);
        }

        public static void SetEULAAccepted(bool value, Branch branch = null)
        {
            if (branch != null)
                IniSettings.Set(branch.branch, "EULA_Accepted", value);
            else
                IniSettings.Set(GetName(false), "EULA_Accepted", value);
        }
    }
} 