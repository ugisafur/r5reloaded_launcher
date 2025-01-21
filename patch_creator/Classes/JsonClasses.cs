namespace patch_creator
{
    internal class Patch
    {
        public List<PatchFile> files { get; set; }
    }

    internal class PatchFile
    {
        public string Name { get; set; }
        public string Action { get; set; }
    }

    public class GameChecksums
    {
        public List<GameFile> files { get; set; }
    }

    public class GameFile
    {
        public string name { get; set; }
        public string checksum { get; set; }
    }

    public class Branch
    {
        public string branch { get; set; }
        public string version { get; set; }
        public string game_url { get; set; }
        public bool enabled { get; set; }
        public bool show_in_launcher { get; set; }
    }

    public class ServerConfig
    {
        public string launcherVersion { get; set; }
        public string launcherSelfUpdater { get; set; }
        public bool allowUpdates { get; set; }
        public List<Branch> branches { get; set; }
    }
}