namespace launcher
{
    public class ComboBranch
    {
        public string title { get; set; }
        public string subtext { get; set; }
        public bool isLocalBranch { get; set; }
    }

    public class Branch
    {
        public string branch { get; set; }
        public string version { get; set; }
        public string lastVersion { get; set; }
        public string game_url { get; set; }
        public string patch_url { get; set; }
        public bool enabled { get; set; }
        public bool show_in_launcher { get; set; }
        public bool allow_updates { get; set; }
        public bool is_local_branch = false;
        public bool update_available = false;
    }

    public class ServerConfig
    {
        public string launcherVersion { get; set; }
        public string launcherSelfUpdater { get; set; }
        public string launcherBackgroundVideo { get; set; }
        public bool launcherallowUpdates { get; set; }
        public List<Branch> branches { get; set; }
    }

    public class LauncherConfig
    {
        public string currentUpdateVersion { get; set; }
        public string currentUpdateBranch { get; set; }
    }

    public class GameFile
    {
        public string name { get; set; }
        public string checksum { get; set; }
    }

    public class FileChecksum
    {
        public string name { get; set; }
        public string checksum { get; set; }
    }

    public class GameFiles
    {
        public List<GameFile> files { get; set; }
    }

    public class GamePatch
    {
        public List<PatchFile> files { get; set; }
    }

    public class PatchFile
    {
        public string Name { get; set; }
        public string Action { get; set; }
    }

    public class GameServerList
    {
        public bool success { get; set; }
        public List<Server> servers { get; set; }
    }

    public class Server
    {
        public string maxPlayers { get; set; }
        public string port { get; set; }
        public string checksum { get; set; }
        public string name { get; set; }
        public string ip { get; set; }
        public string description { get; set; }
        public string hidden { get; set; }
        public string playerCount { get; set; }
        public string playlist { get; set; }
        public string key { get; set; }
        public string region { get; set; }
        public string map { get; set; }
    }
}