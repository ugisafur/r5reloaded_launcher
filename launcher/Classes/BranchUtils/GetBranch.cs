using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using launcher.Classes.CDN;
using launcher.Classes.Global;
using launcher.Classes.Utilities;
using static launcher.Classes.Global.References;

namespace launcher.Classes.BranchUtils
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

        public static string Name(bool uppercase = true)
        {
            return uppercase ? Branch().branch.ToUpper(new CultureInfo("en-US")) : Branch().branch;
        }

        public static string GameURL()
        {
            return Branch().game_url;
        }

        public static bool Enabled()
        {
            return Branch().enabled;
        }

        public static bool ShowInLauncher()
        {
            return Branch().show_in_launcher;
        }

        public static bool AllowUpdates()
        {
            return Branch().allow_updates;
        }

        public static bool IsLocalBranch()
        {
            return Branch().is_local_branch;
        }

        public static bool UpdateAvailable()
        {
            return Branch().update_available;
        }

        public static bool EULAAccepted()
        {
            return Ini.Get(Branch().branch, "EULA_Accepted", false);
        }

        public static bool Installed(Branch branch = null)
        {
            if (branch != null)
                return Ini.Get(branch.branch, "Is_Installed", false);

            return Ini.Get(Branch().branch, "Is_Installed", false);
        }

        public static string LocalVersion()
        {
            return Ini.Get(Branch().branch, "Version", "");
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

        public static string Directory()
        {
            string libraryPath = (string)Ini.Get(Ini.Vars.Library_Location);
            string finalDirectory = Path.Combine(libraryPath, "R5R Library", Name());

            return finalDirectory;
        }
    }
}