using System.Collections.Generic;

namespace launcher.Core.Models
{
    public class GameFiles
    {
        public string game_version { get; set; }
        public string blog_slug { get; set; }
        public List<string> languages { get; set; } = [];
        public List<GameFile> files { get; set; }
    }
} 