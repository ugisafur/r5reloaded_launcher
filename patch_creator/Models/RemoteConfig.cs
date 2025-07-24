using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace patch_creator.Models
{
    public class RemoteConfig
    {
        public string launcherVersion { get; set; } = string.Empty;
        public string launcherSelfUpdater { get; set; } = string.Empty;
        public bool allowUpdates { get; set; }
        public List<ReleaseChannel> channels { get; set; } = [];
    }
}
