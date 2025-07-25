namespace launcher.Core.Models
{
    public class RemoteConfig
    {
        public string launcherVersion { get; set; }
        public string updaterVersion { get; set; }
        public string selfUpdater { get; set; }
        public string backgroundVideo { get; set; }
        public bool allowUpdates { get; set; }
        public bool forceUpdates { get; set; }
        public List<ReleaseChannel> channels { get; set; }
    }
} 