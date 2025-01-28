using launcher.Game;
using launcher.Global;

namespace launcher.BranchUtils
{
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