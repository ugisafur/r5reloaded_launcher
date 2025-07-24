using System.Collections.Generic;

namespace launcher.GameLifecycle.Models
{
    public class GameManifest
    {
        public string game_version { get; set; }
        public string blog_slug { get; set; }
        public List<string> languages { get; set; } = [];
        public List<ManifestEntry> files { get; set; }
    }
} 