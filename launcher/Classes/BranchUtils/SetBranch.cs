namespace launcher.Classes.BranchUtils
{
    public static class SetBranch
    {
        public static void UpdateAvailable(bool value)
        {
            GetBranch.Branch().update_available = value;
        }
    }
}