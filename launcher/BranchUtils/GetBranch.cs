using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using launcher.CDN;
using launcher.Game;
using launcher.Global;
using static launcher.Global.References;

namespace launcher.BranchUtils
{
    public static class GetBranch
    {
        public static int Index()
        {
            int cmbSelectedIndex = appDispatcher.Invoke(() => Branch_Combobox.SelectedIndex);
            return cmbSelectedIndex;
        }

        public static Branch Branch()
        {
            return Configuration.ServerConfig.branches[Index()];
        }

        public static string Name(bool uppercase = true, Branch branch = null)
        {
            if (branch != null)
                return uppercase ? branch.branch.ToUpper(new CultureInfo("en-US")) : branch.branch;

            return uppercase ? Branch().branch.ToUpper(new CultureInfo("en-US")) : Branch().branch;
        }

        public static string GameURL(Branch branch = null)
        {
            if (branch != null)
                return branch.game_url;

            return Branch().game_url;
        }

        public static bool Enabled(Branch branch = null)
        {
            if (branch != null)
                return branch.enabled;

            return Branch().enabled;
        }

        public static bool ShowInLauncher(Branch branch = null)
        {
            if (branch != null)
                return branch.show_in_launcher;

            return Branch().show_in_launcher;
        }

        public static bool AllowUpdates(Branch branch = null)
        {
            if (branch != null)
                return branch.allow_updates;

            return Branch().allow_updates;
        }

        public static bool IsLocalBranch(Branch branch = null)
        {
            if (branch != null)
                return branch.is_local_branch;

            return Branch().is_local_branch;
        }

        public static bool UpdateAvailable(Branch branch = null)
        {
            if (branch != null)
                return branch.update_available;

            return Branch().update_available;
        }

        public static bool EULAAccepted(Branch branch = null)
        {
            if (branch != null)
                return (bool)Ini.Get(branch.branch, "EULA_Accepted", false);

            return (bool)Ini.Get(Branch().branch, "EULA_Accepted", false);
        }

        public static bool DownloadHDTextures(Branch branch = null)
        {
            if (branch != null)
                return (bool)Ini.Get(branch.branch, "Download_HD_Textures", false);

            return (bool)Ini.Get(Branch().branch, "Download_HD_Textures", false);
        }

        public static bool Installed(Branch branch = null)
        {
            if (branch != null)
                return (bool)Ini.Get(branch.branch, "Is_Installed", false);

            return (bool)Ini.Get(Branch().branch, "Is_Installed", false);
        }

        public static string LocalVersion(Branch branch = null)
        {
            if (branch != null)
                return (string)Ini.Get(branch.branch, "Version", "");

            return (string)Ini.Get(Branch().branch, "Version", "");
        }

        public static string ServerComboVersion(Branch branch)
        {
            if (branch.is_local_branch)
                return "Local Install";

            return Fetch.GameVersion(branch.game_url);
        }

        public static string ServerVersion()
        {
            return Fetch.GameVersion(Branch().game_url);
        }

        public static string Directory(Branch branch = null)
        {
            if (branch != null)
                return Path.Combine((string)Ini.Get(Ini.Vars.Library_Location), "R5R Library", Name(true, branch));

            return Path.Combine((string)Ini.Get(Ini.Vars.Library_Location), "R5R Library", Name()); ;
        }

        public static bool ExeExists(Branch branch = null)
        {
            if (branch != null)
            {
                return System.IO.Directory.Exists(Directory(branch)) && File.Exists(Path.Combine(Directory(branch), "r5apex.exe"));
            }

            return System.IO.Directory.Exists(Directory()) && File.Exists(Path.Combine(Directory(), "r5apex.exe"));
        }
    }
}