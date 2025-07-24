using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace patch_creator.Models
{
    public class GameManifest
    {
        public string game_version { get; set; } = string.Empty;
        public string blog_slug { get; set; } = string.Empty;
        public List<string> languages { get; set; } = [];
        public List<ManifestEntry> files { get; set; } = [];
    }
}
