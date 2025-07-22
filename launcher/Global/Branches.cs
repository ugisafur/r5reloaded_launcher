using System.Globalization;
using System.IO;
using launcher.Game;
using static launcher.Global.References;

namespace launcher.Global
{
    public static class GetBranch
    {
        public static int Index() { return appDispatcher.Invoke(() => Branch_Combobox.SelectedIndex); }

        public static Branch Branch() { return Launcher.ServerConfig.branches[Index()]; }

        public static bool Enabled(Branch branch = null) { return branch != null ? branch.enabled : Branch().enabled; }
        public static bool AllowUpdates(Branch branch = null) { return branch != null ? branch.allow_updates : Branch().allow_updates; }
        public static bool IsLocalBranch(Branch branch = null) { return branch != null ? branch.is_local_branch : Branch().is_local_branch; }
        public static bool UpdateAvailable(Branch branch = null) { return branch != null ? branch.update_available : Branch().update_available; }
        public static bool EULAAccepted(Branch branch = null) { return branch != null ? (bool)Ini.Get(branch.branch, "EULA_Accepted", false) : (bool)Ini.Get(Branch().branch, "EULA_Accepted", false); }
        public static bool DownloadHDTextures(Branch branch = null) { return branch != null ? (bool)Ini.Get(branch.branch, "Download_HD_Textures", false) : (bool)Ini.Get(Branch().branch, "Download_HD_Textures", false); }
        public static bool Installed(Branch branch = null) { return branch != null ? (bool)Ini.Get(branch.branch, "Is_Installed", false) : (bool)Ini.Get(Branch().branch, "Is_Installed", false); }
        public static bool ExeExists(Branch branch = null) { return branch != null ? System.IO.Directory.Exists(Directory(branch)) && File.Exists(Path.Combine(Directory(branch), "r5apex.exe")) : System.IO.Directory.Exists(Directory()) && File.Exists(Path.Combine(Directory(), "r5apex.exe")); }

        public static string LocalVersion(Branch branch = null) { return branch != null ? (string)Ini.Get(branch.branch, "Version", "") : (string)Ini.Get(Branch().branch, "Version", ""); }
        public static string ServerComboVersion(Branch branch) { return branch.is_local_branch ? "Local Install" : Fetch.GameVersion(branch.game_url); }
        public static string ServerVersion() { return Fetch.GameVersion(Branch().game_url); }
        public static string Directory(Branch branch = null) { return branch != null ? Path.Combine((string)Ini.Get(Ini.Vars.Library_Location), "R5R Library", Name(true, branch)) : Path.Combine((string)Ini.Get(Ini.Vars.Library_Location), "R5R Library", Name()); }
        public static string DediURL(Branch branch = null) { return branch != null ? branch.dedi_url : Branch().dedi_url; }
        public static string Name(bool uppercase = true, Branch branch = null) { return branch != null ? (uppercase ? branch.branch.ToUpper(new CultureInfo("en-US")) : branch.branch) : (uppercase ? Branch().branch.ToUpper(new CultureInfo("en-US")) : Branch().branch); }
        public static string GameURL(Branch branch = null) { return branch != null ? branch.game_url : Branch().game_url; }

        public async static Task<string> BlogSlug(Branch branch = null)
        {
            GameFiles gameFiles = await Fetch.GameFiles(false);

            return gameFiles.blog_slug; 
        }
    }

    public static class SetBranch
    {
        public static void UpdateAvailable(bool value, Branch branch = null)
        {
            if (branch != null)
                branch.update_available = value;
            else
                GetBranch.Branch().update_available = value;
        }

        public static void DownloadHDTextures(bool value, Branch branch = null)
        {
            if (branch != null)
                Ini.Set(branch.branch, "Download_HD_Textures", value);
            else
                Ini.Set(GetBranch.Name(false), "Download_HD_Textures", value);
        }

        public static void Installed(bool value, Branch branch = null)
        {
            if (branch != null)
                Ini.Set(branch.branch, "Is_Installed", value);
            else
                Ini.Set(GetBranch.Name(false), "Is_Installed", value);
        }

        public static void Version(string value, Branch branch = null)
        {
            if (branch != null)
                Ini.Set(branch.branch, "Version", value);
            else
                Ini.Set(GetBranch.Name(false), "Version", value);
        }

        public static void EULAAccepted(bool value, Branch branch = null)
        {
            if (branch != null)
                Ini.Set(branch.branch, "EULA_Accepted", value);
            else
                Ini.Set(GetBranch.Name(false), "EULA_Accepted", value);
        }
    }
}
