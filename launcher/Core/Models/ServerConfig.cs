using System.Collections.Generic;

namespace launcher.Core.Models
{
    public class ServerConfig
    {
        public string launcherVersion { get; set; }
        public string updaterVersion { get; set; }
        public string selfUpdater { get; set; }
        public string backgroundVideo { get; set; }
        public bool allowUpdates { get; set; }
        public bool forceUpdates { get; set; }
        public List<Branch> branches { get; set; }
    }
} 